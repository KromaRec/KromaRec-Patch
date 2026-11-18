#!/usr/bin/env python3
"""Run the backend room regression with encrypted NameServer auth over loopback.

Uses the release's own wire-test fixtures and a test-only cryptography dependency.
"""
import argparse
from hashlib import sha256
from pathlib import Path
import secrets
import struct
import sys

from cryptography.hazmat.primitives import padding
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes


OAKLEY_768 = int(
    "FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E08"
    "8A67CC74020BBEA63B139B22514A08798E3404DDEF9519B3CD3A431B"
    "302B0A6DF25F14374FE1356D6D51C245E485B576625E7EC6F44C42E9"
    "A63A3620FFFFFFFFFFFFFFFF", 16
)


def big_endian(value):
    return value.to_bytes((value.bit_length() + 7) // 8, "big")


def main():
    cli = argparse.ArgumentParser(description=__doc__)
    cli.add_argument("release", type=Path, help="extracted KromaRec release root")
    cli.add_argument("--glibc", action="store_true")
    args = cli.parse_args()
    root = args.release.resolve()
    sys.path.insert(0, str(root / "tools"))
    import luxon_smoke as wire
    import rooms_smoke

    counts = {230: 0, 231: 0}

    def encrypted_auth(peer, user, app, ticket, reject=False, operation=230):
        exponent = secrets.randbelow(OAKLEY_768 - 3) + 2
        public_key = big_endian(pow(22, exponent, OAKLEY_768))
        peer.reliable(bytes((243, 6, 0, 1, 1)) + wire.encode(public_key))
        handshake = peer.wait(lambda msg: msg and msg[:3] == bytes((243, 7, 0)))
        assert struct.unpack_from("<h", handshake, 3)[0] == 0, "Photon encryption handshake"
        reader = wire.Reader(handshake[5:])
        reader.value()
        server_key = int.from_bytes(reader.fields()[1], "big")
        assert 1 < server_key < OAKLEY_768 - 1
        key = sha256(big_endian(pow(server_key, exponent, OAKLEY_768))).digest()
        cipher = Cipher(algorithms.AES(key), modes.CBC(bytes(16)))
        fields = {224: app, 220: "20230414", 225: user}
        if ticket:
            fields[216] = "token=" + ticket
            fields[217] = 0
        if operation == 231:
            fields[193] = 0  # PayloadEncryption
            fields[195] = 0  # UDP
        payload = bytes((operation, len(fields))) + b"".join(
            bytes((key,)) + wire.encode(value) for key, value in fields.items()
        )
        pad = padding.PKCS7(128).padder()
        encrypt = cipher.encryptor()
        peer.reliable(b"\xf3\x82" + encrypt.update(pad.update(payload) + pad.finalize()) + encrypt.finalize())
        response = peer.wait(lambda msg: msg and msg[:2] == b"\xf3\x83")
        decrypt = cipher.decryptor()
        unpad = padding.PKCS7(128).unpadder()
        data = unpad.update(decrypt.update(response[2:]) + decrypt.finalize()) + unpad.finalize()
        assert data[0] == operation
        status = struct.unpack_from("<h", data, 1)[0]
        if reject:
            assert status != 0, (operation, status)
            return status
        assert status == 0, (operation, status)
        counts[operation] += 1
        result = wire.Reader(data[3:])
        result.value()
        return result.fields()

    wire.authenticate = encrypted_auth
    binary = root / "bin" / ("kromarec-x86_64-linux-gnu" if args.glibc else "kromarec")
    rooms_smoke.run(binary, baseline=None, secure_luxon=root / "bin/luxon-kromarec-x86_64-linux-gnu")
    assert counts[230] > 0 and counts[231] > 0
    print(f"PASS | encrypted NameServer responses accepted: Authenticate={counts[230]}, AuthenticateOnce={counts[231]}")


if __name__ == "__main__":
    main()
