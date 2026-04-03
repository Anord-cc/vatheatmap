import math
import os
import secrets
import sqlite3
import time
from functools import wraps
from urllib.parse import urlencode

import requests
from dotenv import load_dotenv
from flask import Flask, jsonify, redirect, request, send_from_directory, session
from flask_cors import CORS

load_dotenv()

app = Flask(__name__, static_folder="../frontend/static", template_folder="../frontend/templates")
app.secret_key = os.environ.get("SECRET_KEY", secrets.token_hex(32))
CORS(app, supports_credentials=True)

# ── Discord OAuth Config ──────────────────────────────────────────────────────
DISCORD_AUTH_URL      = "https://discord.com/oauth2/authorize"
DISCORD_TOKEN_URL     = "https://discord.com/api/oauth2/token"
DISCORD_USER_URL      = "https://discord.com/api/users/@me"
DISCORD_CLIENT_ID     = os.environ.get("DISCORD_CLIENT_ID")
DISCORD_CLIENT_SECRET = os.environ.get("DISCORD_CLIENT_SECRET")
DISCORD_REDIRECT_URI  = os.environ.get("DISCORD_REDIRECT_URI")

# ── Allowlist — only these Discord user IDs may log in ───────────────────────
ALLOWED_DISCORD_IDS = {
    discord_id.strip()
    for discord_id in (os.environ.get("ALLOWED_DISCORD_IDS") or "").split(",")
    if discord_id.strip()
}

# ── VATSIM public data feed ───────────────────────────────────────────────────
VATSIM_DATA_URL = "https://data.vatsim.net/v3/vatsim-data.json"
_vatsim_cache   = {"data": None, "fetched_at": 0}
VATSIM_CACHE_TTL_SECONDS = max(5, int(os.environ.get("VATSIM_CACHE_TTL_SECONDS", "15")))
MAX_SEGMENT_GAP_SECONDS = 60 * 60 * 4
MAX_SEGMENT_DISTANCE_KM = 900

def get_vatsim_data():
    now = time.time()
    if now - _vatsim_cache["fetched_at"] < VATSIM_CACHE_TTL_SECONDS:
        return _vatsim_cache["data"]
    try:
        r = requests.get(VATSIM_DATA_URL, timeout=10)
        r.raise_for_status()
        _vatsim_cache["data"] = r.json()
        _vatsim_cache["fetched_at"] = now
    except Exception as e:
        print(f"VATSIM data fetch error: {e}")
    return _vatsim_cache["data"]

def find_pilot(vatsim_id: str):
    data = get_vatsim_data()
    if not data:
        return None
    for pilot in data.get("pilots", []):
        if str(pilot.get("cid")) == str(vatsim_id):
            return pilot
    return None


def close_active_flight(conn, vatsim_id: str):
    conn.execute(
        """
        UPDATE flights
        SET ended_at = CURRENT_TIMESTAMP
        WHERE vatsim_id = ? AND ended_at IS NULL
        """,
        (vatsim_id,),
    )


def great_circle_distance_km(lat1, lng1, lat2, lng2):
    radius_km = 6371.0
    phi1 = math.radians(lat1)
    phi2 = math.radians(lat2)
    delta_phi = math.radians(lat2 - lat1)
    delta_lambda = math.radians(lng2 - lng1)

    a = (
        math.sin(delta_phi / 2) ** 2
        + math.cos(phi1) * math.cos(phi2) * math.sin(delta_lambda / 2) ** 2
    )
    return 2 * radius_km * math.atan2(math.sqrt(a), math.sqrt(1 - a))


def should_split_segment(previous, current):
    if previous is None:
        return True

    prev_ts = previous["recorded_unix"] or 0
    curr_ts = current["recorded_unix"] or 0
    if curr_ts - prev_ts > MAX_SEGMENT_GAP_SECONDS:
        return True

    distance_km = great_circle_distance_km(
        previous["lat"], previous["lng"], current["lat"], current["lng"]
    )
    return distance_km > MAX_SEGMENT_DISTANCE_KM


def build_track_segments(rows):
    segments = []
    points = []
    current_segment = []
    previous = None

    for row in rows:
        point = {
            "lat": row["lat"],
            "lng": row["lng"],
            "callsign": row["callsign"],
            "altitude": row["altitude"],
            "groundspeed": row["groundspeed"],
            "recorded_at": row["recorded_at"],
            "recorded_unix": row["recorded_unix"],
        }
        points.append(point)

        if should_split_segment(previous, point):
            if len(current_segment) > 1:
                segments.append(current_segment)
            current_segment = [[point["lat"], point["lng"]]]
        else:
            current_segment.append([point["lat"], point["lng"]])

        previous = point

    if len(current_segment) > 1:
        segments.append(current_segment)

    return points, segments

# ── Database ──────────────────────────────────────────────────────────────────
DB_PATH = os.path.join(os.path.dirname(__file__), "flights.db")

def get_db():
    conn = sqlite3.connect(DB_PATH)
    conn.row_factory = sqlite3.Row
    return conn

def init_db():
    with get_db() as conn:
        conn.executescript("""
            CREATE TABLE IF NOT EXISTS users (
                id           INTEGER PRIMARY KEY AUTOINCREMENT,
                discord_id   TEXT UNIQUE NOT NULL,
                discord_name TEXT,
                vatsim_id    TEXT UNIQUE,
                created_at   DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS flight_points (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                vatsim_id   TEXT NOT NULL,
                callsign    TEXT,
                lat         REAL NOT NULL,
                lng         REAL NOT NULL,
                altitude    INTEGER,
                groundspeed INTEGER,
                recorded_at DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS flights (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                vatsim_id   TEXT NOT NULL,
                callsign    TEXT,
                dep         TEXT,
                arr         TEXT,
                aircraft    TEXT,
                started_at  DATETIME DEFAULT CURRENT_TIMESTAMP,
                ended_at    DATETIME
            );

            CREATE INDEX IF NOT EXISTS idx_points_vatsim  ON flight_points(vatsim_id);
            CREATE INDEX IF NOT EXISTS idx_flights_vatsim ON flights(vatsim_id);
        """)

init_db()

# ── Auth decorators ───────────────────────────────────────────────────────────
def require_auth(fn):
    @wraps(fn)
    def wrapper(*args, **kwargs):
        if not session.get("discord_id"):
            return jsonify({"error": "unauthorized"}), 401
        return fn(*args, **kwargs)
    return wrapper

def require_linked(fn):
    @wraps(fn)
    def wrapper(*args, **kwargs):
        if not session.get("discord_id"):
            return jsonify({"error": "unauthorized"}), 401
        if not session.get("vatsim_id"):
            return jsonify({"error": "no_vatsim_linked"}), 403
        return fn(*args, **kwargs)
    return wrapper

# ── Discord OAuth ─────────────────────────────────────────────────────────────
@app.route("/auth/login")
def login():
    state = secrets.token_urlsafe(16)
    session["oauth_state"] = state
    params = {
        "client_id":     DISCORD_CLIENT_ID,
        "redirect_uri":  DISCORD_REDIRECT_URI,
        "response_type": "code",
        "scope":         "identify",
        "state":         state,
    }
    return redirect(f"{DISCORD_AUTH_URL}?{urlencode(params)}")

@app.route("/auth/callback")
def callback():
    if request.args.get("error"):
        return redirect(f"/?error={request.args.get('error')}")

    code  = request.args.get("code")
    state = request.args.get("state")
    if state != session.pop("oauth_state", None):
        return redirect("/?error=state_mismatch")

    # Exchange code for token
    try:
        token_resp = requests.post(DISCORD_TOKEN_URL, data={
            "grant_type":    "authorization_code",
            "client_id":     DISCORD_CLIENT_ID,
            "client_secret": DISCORD_CLIENT_SECRET,
            "redirect_uri":  DISCORD_REDIRECT_URI,
            "code":          code,
        }, headers={"Content-Type": "application/x-www-form-urlencoded"}, timeout=10)
        token_resp.raise_for_status()
        tokens = token_resp.json()
    except Exception as e:
        print(f"Discord token error: {e}")
        return redirect("/?error=token_exchange_failed")

    # Fetch Discord user info
    try:
        user_resp = requests.get(DISCORD_USER_URL, headers={
            "Authorization": f"Bearer {tokens['access_token']}"
        }, timeout=10)
        user_resp.raise_for_status()
        discord_user = user_resp.json()
    except Exception:
        return redirect("/?error=user_fetch_failed")

    discord_id   = str(discord_user.get("id", ""))
    discriminator = discord_user.get("discriminator", "0")
    discord_name  = discord_user.get("username", "")
    if discriminator and discriminator != "0":
        discord_name = f"{discord_name}#{discriminator}"

    # ── Allowlist gate ────────────────────────────────────────────────────────
    if discord_id not in ALLOWED_DISCORD_IDS:
        return redirect("/?error=access_denied")

    # Upsert user
    with get_db() as conn:
        conn.execute("""
            INSERT INTO users (discord_id, discord_name)
            VALUES (?, ?)
            ON CONFLICT(discord_id) DO UPDATE SET discord_name = excluded.discord_name
        """, (discord_id, discord_name))
        row = conn.execute(
            "SELECT vatsim_id FROM users WHERE discord_id = ?", (discord_id,)
        ).fetchone()
        vatsim_id = row["vatsim_id"] if row else None

    session["discord_id"]   = discord_id
    session["discord_name"] = discord_name
    session["vatsim_id"]    = vatsim_id

    return redirect("/link-vatsim" if not vatsim_id else "/dashboard")

@app.route("/auth/logout")
def logout():
    session.clear()
    return redirect("/")

# ── VATSIM Linking ────────────────────────────────────────────────────────────
@app.route("/api/link-vatsim", methods=["POST"])
@require_auth
def link_vatsim():
    discord_id = session["discord_id"]
    data       = request.get_json() or {}
    vatsim_id  = str(data.get("vatsim_id", "")).strip()

    if not vatsim_id.isdigit() or len(vatsim_id) < 4:
        return jsonify({"error": "Invalid VATSIM CID — must be a number (e.g. 1234567)"}), 400

    try:
        with get_db() as conn:
            conn.execute(
                "UPDATE users SET vatsim_id = ? WHERE discord_id = ?",
                (vatsim_id, discord_id)
            )
    except sqlite3.IntegrityError:
        return jsonify({"error": "That VATSIM CID is already linked to another account"}), 409

    session["vatsim_id"] = vatsim_id
    return jsonify({"ok": True, "vatsim_id": vatsim_id})

@app.route("/api/unlink-vatsim", methods=["POST"])
@require_auth
def unlink_vatsim():
    with get_db() as conn:
        conn.execute(
            "UPDATE users SET vatsim_id = NULL WHERE discord_id = ?",
            (session["discord_id"],)
        )
    session["vatsim_id"] = None
    return jsonify({"ok": True})

# ── User info ─────────────────────────────────────────────────────────────────
@app.route("/api/me")
def me():
    if not session.get("discord_id"):
        return jsonify({"authenticated": False}), 401
    return jsonify({
        "authenticated": True,
        "discord_id":    session["discord_id"],
        "discord_name":  session.get("discord_name", ""),
        "vatsim_id":     session.get("vatsim_id"),
    })

# ── Flight data ───────────────────────────────────────────────────────────────
@app.route("/api/live")
@require_linked
def live_flight():
    vatsim_id = session["vatsim_id"]
    pilot = find_pilot(vatsim_id)

    if not pilot:
        with get_db() as conn:
            close_active_flight(conn, vatsim_id)
        return jsonify({"online": False})

    with get_db() as conn:
        conn.execute("""
            INSERT INTO flight_points (vatsim_id, callsign, lat, lng, altitude, groundspeed)
            VALUES (?, ?, ?, ?, ?, ?)
        """, (
            vatsim_id, pilot.get("callsign"),
            pilot.get("latitude"), pilot.get("longitude"),
            pilot.get("altitude"), pilot.get("groundspeed"),
        ))
        fp = pilot.get("flight_plan") or {}
        conn.execute("""
            INSERT INTO flights (vatsim_id, callsign, dep, arr, aircraft)
            SELECT ?, ?, ?, ?, ?
            WHERE NOT EXISTS (
                SELECT 1 FROM flights WHERE vatsim_id = ? AND ended_at IS NULL
            )
        """, (
            vatsim_id, pilot.get("callsign"),
            fp.get("departure",""), fp.get("arrival",""), fp.get("aircraft_short",""),
            vatsim_id,
        ))

    return jsonify({
        "online":      True,
        "callsign":    pilot.get("callsign"),
        "lat":         pilot.get("latitude"),
        "lng":         pilot.get("longitude"),
        "altitude":    pilot.get("altitude"),
        "groundspeed": pilot.get("groundspeed"),
        "heading":     pilot.get("heading"),
        "dep":         (pilot.get("flight_plan") or {}).get("departure",""),
        "arr":         (pilot.get("flight_plan") or {}).get("arrival",""),
        "aircraft":    (pilot.get("flight_plan") or {}).get("aircraft_short",""),
    })

@app.route("/api/heatmap")
@require_linked
def heatmap():
    vatsim_id = session["vatsim_id"]
    with get_db() as conn:
        rows = conn.execute("""
            SELECT lat, lng, COUNT(*) as weight
            FROM flight_points WHERE vatsim_id = ?
            GROUP BY ROUND(lat,2), ROUND(lng,2)
        """, (vatsim_id,)).fetchall()
        track_rows = conn.execute(
            """
            SELECT
                callsign,
                lat,
                lng,
                altitude,
                groundspeed,
                recorded_at,
                CAST(strftime('%s', recorded_at) AS INTEGER) AS recorded_unix
            FROM flight_points
            WHERE vatsim_id = ?
            ORDER BY recorded_at ASC, id ASC
            """,
            (vatsim_id,),
        ).fetchall()

    ordered_points, segments = build_track_segments(track_rows)
    recent_track = [
        {"lat": point["lat"], "lng": point["lng"], "recorded_at": point["recorded_at"]}
        for point in ordered_points[-120:]
    ]

    bounds = None
    if ordered_points:
        latitudes = [point["lat"] for point in ordered_points]
        longitudes = [point["lng"] for point in ordered_points]
        bounds = {
            "southWest": [min(latitudes), min(longitudes)],
            "northEast": [max(latitudes), max(longitudes)],
        }

    return jsonify({
        "points": [{"lat": r["lat"], "lng": r["lng"], "weight": r["weight"]} for r in rows],
        "segments": segments,
        "recent_track": recent_track,
        "bounds": bounds,
        "totals": {
            "track_points": len(ordered_points),
            "segments": len(segments),
        },
    })

@app.route("/api/flights")
@require_linked
def flights():
    vatsim_id = session["vatsim_id"]
    with get_db() as conn:
        rows = conn.execute("""
            SELECT callsign, dep, arr, aircraft, started_at, ended_at
            FROM flights WHERE vatsim_id = ?
            ORDER BY started_at DESC LIMIT 20
        """, (vatsim_id,)).fetchall()
    return jsonify({"flights": [dict(r) for r in rows]})

@app.route("/api/stats")
@require_linked
def stats():
    vatsim_id = session["vatsim_id"]
    with get_db() as conn:
        total_points  = conn.execute(
            "SELECT COUNT(*) as c FROM flight_points WHERE vatsim_id=?", (vatsim_id,)
        ).fetchone()["c"]
        total_flights = conn.execute(
            "SELECT COUNT(*) as c FROM flights WHERE vatsim_id=?", (vatsim_id,)
        ).fetchone()["c"]
        top_routes = conn.execute("""
            SELECT dep, arr, COUNT(*) as times
            FROM flights WHERE vatsim_id=? AND dep!='' AND arr!=''
            GROUP BY dep, arr ORDER BY times DESC LIMIT 5
        """, (vatsim_id,)).fetchall()
    return jsonify({
        "total_points":  total_points,
        "total_flights": total_flights,
        "top_routes":    [dict(r) for r in top_routes],
    })

# ── Serve frontend ────────────────────────────────────────────────────────────
@app.route("/")
def index():
    return send_from_directory("../frontend", "index.html")

@app.route("/dashboard")
def dashboard():
    return send_from_directory("../frontend", "dashboard.html")

@app.route("/link-vatsim")
def link_vatsim_page():
    return send_from_directory("../frontend", "link-vatsim.html")

@app.route("/<path:path>")
def static_files(path):
    return send_from_directory("../frontend", path)

if __name__ == "__main__":
    app.run(debug=True, port=5000)
