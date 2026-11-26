#!/usr/bin/env python3
"""Hinchas de relleno del Ranking de la hinchada (pedido del usuario, 2026-11-26).

Crea o borra 20 filas ficticias en leaderboard/ de haychoriypaty para que la tabla no se vea vacía
mientras hay pocos jugadores reales. Son temporales: se borran con `delete` cuando haya hinchas de verdad.

Usa solo la sesión oficial del CLI de Firebase (su servidor MCP: firestore_* tools); nunca lee ni muestra tokens.
El proyecto está fijo a propósito. Uso:
    python3 -I tools/firebase/leaderboard_filler.py list|create|delete
CLI: variable FIREBASE_CLI, o `firebase` en el PATH, o /private/tmp/hcyp-firebase-cli/firebase.
"""
import datetime
import json
import os
import shutil
import subprocess
import sys

PROJECT = "haychoriypaty"
PARENT = f"projects/{PROJECT}/databases/(default)/documents"
COLLECTION = "leaderboard"
ID_PREFIX = "relleno-hinchada-"

# apodo, canchas ganadas, victorias repetidas (Puntos de Liga = 100 * canchas + 5 * repetidas, repetidas <= 9 * canchas).
FILLERS = [
    ("Morcipan_10", 14, 98), ("La Chimi", 14, 69), ("Tano Parrilla", 13, 58), ("Fernetero", 13, 37),
    ("Doña Provoleta", 12, 44), ("El Colo Asado", 12, 21), ("Bondiolero", 11, 40), ("Vacío_Feliz", 11, 19),
    ("Mollejita", 10, 33), ("Pancho.LP", 10, 12), ("Hinchada Sur", 9, 27), ("Lomito Loco", 9, 8),
    ("Chori-Maestro", 8, 22), ("El Ruso 22", 7, 15), ("Garrapiñada", 6, 18), ("Flaco Paty", 5, 9),
    ("Turco Asado", 4, 12), ("Chimichurri", 3, 6), ("Panchito_23", 2, 5), ("Picadita", 2, 0),
]


def filler_rows():
    for index, (nickname, won, repeats) in enumerate(FILLERS, start=1):
        assert 0 < won <= 14 and 0 <= repeats <= 9 * won and 3 <= len(nickname) <= 16
        yield ID_PREFIX + f"{index:02d}", nickname, won, 100 * won + 5 * repeats


def cli_path():
    for candidate in (os.environ.get("FIREBASE_CLI"), shutil.which("firebase"), "/private/tmp/hcyp-firebase-cli/firebase"):
        if candidate and os.path.exists(candidate):
            return candidate
    sys.exit("Firebase CLI not found; set FIREBASE_CLI. No changes made.")


class FirestoreMcp:
    def __init__(self):
        root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
        self.proc = subprocess.Popen([cli_path(), "mcp", "--only", "firestore", "--dir", root],
                                     stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, text=True)
        self.next_id = 0
        self.request("initialize", {"protocolVersion": "2024-11-05", "capabilities": {}, "clientInfo": {"name": "leaderboard-filler", "version": "1"}})
        self.notify("notifications/initialized")
        project = self.tool("firebase_update_environment", {"active_project": PROJECT}, check=False)
        if project.get("isError"):
            sys.exit("Could not select project " + PROJECT + ". No changes made.")

    def notify(self, method):
        self.proc.stdin.write(json.dumps({"jsonrpc": "2.0", "method": method}) + "\n")
        self.proc.stdin.flush()

    def request(self, method, params):
        self.next_id += 1
        self.proc.stdin.write(json.dumps({"jsonrpc": "2.0", "id": self.next_id, "method": method, "params": params}) + "\n")
        self.proc.stdin.flush()
        while True:
            line = self.proc.stdout.readline()
            if not line:
                sys.exit("Firebase MCP server closed unexpectedly.")
            try:
                message = json.loads(line)
            except ValueError:
                continue
            if message.get("id") == self.next_id:
                if "error" in message:
                    raise RuntimeError(message["error"].get("message", "MCP error"))
                return message["result"]

    def tool(self, name, arguments, check=True):
        result = self.request("tools/call", {"name": name, "arguments": arguments})
        if check and result.get("isError"):
            raise RuntimeError(" ".join(c.get("text", "") for c in result.get("content", []))[:400])
        return result

    def close(self):
        self.proc.kill()


def text_of(result):
    return " ".join(c.get("text", "") for c in result.get("content", []))


def list_rows(mcp):
    result = mcp.tool("firestore_list_documents", {"parent": PARENT, "collectionId": COLLECTION, "pageSize": 300})
    body = text_of(result)
    try:
        documents = json.loads(body).get("documents", [])
    except ValueError:
        sys.exit("Unexpected list answer: " + body[:300])
    rows = []
    for document in documents:
        fields = document.get("fields", {})
        rows.append((document["name"].rsplit("/", 1)[-1], fields.get("nickname", {}).get("stringValue", "?"),
                     int(fields.get("points", {}).get("integerValue", 0))))
    return sorted(rows, key=lambda row: -row[2])


def main():
    command = sys.argv[1] if len(sys.argv) > 1 else "list"
    if command not in ("list", "create", "delete"):
        sys.exit("Use list, create or delete.")
    mcp = FirestoreMcp()
    try:
        existing = {row[0] for row in list_rows(mcp)}
        if command == "create":
            now = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%fZ")
            for doc_id, nickname, won, points in filler_rows():
                if doc_id in existing:
                    print("skip (already there):", doc_id)
                    continue
                mcp.tool("firestore_add_document", {"parent": PARENT, "collectionId": COLLECTION, "documentId": doc_id, "document": {"fields": {
                    "schemaVersion": {"integerValue": "1"}, "uid": {"stringValue": doc_id}, "nickname": {"stringValue": nickname},
                    "points": {"integerValue": str(points)}, "wonLevels": {"integerValue": str(won)}, "updatedAt": {"timestampValue": now}}}})
                print("created:", doc_id, nickname, points)
        elif command == "delete":
            for doc_id, _, _, _ in filler_rows():
                if doc_id not in existing:
                    continue
                mcp.tool("firestore_delete_document", {"name": f"{PARENT}/{COLLECTION}/{doc_id}"})
                print("deleted:", doc_id)
        rows = list_rows(mcp)
        real = sum(1 for row in rows if not row[0].startswith(ID_PREFIX))
        print(f"leaderboard: {len(rows)} rows ({real} real, {len(rows) - real} relleno)")
        for position, (doc_id, nickname, points) in enumerate(rows[:25], start=1):
            print(f"{position:>3}. {nickname:<16} {points:>5}{'  (relleno)' if doc_id.startswith(ID_PREFIX) else ''}")
    finally:
        mcp.close()


if __name__ == "__main__":
    main()
