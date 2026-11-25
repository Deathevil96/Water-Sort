---
name: signing-key-in-repo-cleanup
description: Encrypted signing keystore + passphrase were pushed to the repo for Mac handoff; delete them once the key is set in Unity
metadata:
  type: project
---

On 2026-11-25 the user had the release keystore bundle (`docs/signing.tar.gz.enc`) and its passphrase (`docs/signing-passphrase.txt`) pushed to GitHub (Deathevil96/Water-Sort) so they could continue on a MacBook without copy-pasting.

**Why:** the user had no time to transfer the key manually; convenience chosen over secrecy.

**How to apply:** once the user says the signing key is configured in Unity on the Mac, `git rm` both files, update `docs/HANDOFF.md`, push. Remind them they remain in git history (rotate/reset the upload key via Play Console if the repo is public). See [[release-versioning-and-builds]].
