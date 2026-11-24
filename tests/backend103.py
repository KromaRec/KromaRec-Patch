"""Run the managed consumer against an extracted, disposable Linux 0.10.3 backend."""
import argparse
import os
from pathlib import Path
import secrets
import socket
import subprocess
import tempfile

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("release", type=Path)
release = parser.parse_args().release.resolve()
with tempfile.TemporaryDirectory() as work:
    with socket.socket() as listener:
        listener.bind(("127.0.0.1", 0))
        port = listener.getsockname()[1]
    env = {k: v for k, v in os.environ.items() if not k.startswith("KROMAREC_")}
    env.update(KROMAREC_DOMAIN="rec.test", KROMAREC_BIND=f"127.0.0.1:{port}",
               KROMAREC_PASSWORD_SIGNUP="true", KROMAREC_JWT_SECRET=secrets.token_hex(32),
               KROMAREC_DB=str(Path(work) / "client-test.db"), KROMAREC_PHOTON_REGION="us",
               KROMAREC_PHOTON_REALTIME_APP_ID="7a6eafd9-8a49-47bd-98da-c743525a3e72")
    process = subprocess.Popen([release / "bin/kromarec"], cwd=release, env=env, stderr=subprocess.PIPE)
    try:
        if b"KromaRec listening" not in process.stderr.readline():
            raise RuntimeError("Disposable server startup failed")
        subprocess.run(["dotnet", "run", "--project", "tests/ProtocolChecks.csproj", "-c", "Release",
                        "--", "--disposable-loopback-port", str(port)],
                       cwd=Path(__file__).resolve().parents[1], check=True, timeout=90)
    finally:
        process.terminate()
        process.wait(timeout=15)
