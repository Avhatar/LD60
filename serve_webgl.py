"""Static server for locally testing a Unity WebGL build.

Usage:  python serve_webgl.py [path-to-build-folder] [port]

Plain `python -m http.server` breaks on Brotli/Gzip-compressed Unity builds
because it sends no Content-Encoding header. This one does.
"""

import http.server
import os
import socketserver
import sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else "."
PORT = int(sys.argv[2]) if len(sys.argv) > 2 else 8000


class UnityHandler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        if self.path.endswith(".br"):
            self.send_header("Content-Encoding", "br")
        elif self.path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        super().end_headers()

    def guess_type(self, path):
        base = path[:-3] if path.endswith((".br", ".gz")) else path
        if base.endswith(".wasm"):
            return "application/wasm"
        if base.endswith(".js"):
            return "application/javascript"
        if base.endswith(".data"):
            return "application/octet-stream"
        return super().guess_type(path)


socketserver.TCPServer.allow_reuse_address = True

os.chdir(ROOT)
with socketserver.TCPServer(("", PORT), UnityHandler) as httpd:
    print(f"Serving {os.path.abspath(ROOT)} at http://localhost:{PORT}")
    httpd.serve_forever()
