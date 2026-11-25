---
name: signing-key-in-repo-cleanup
description: Old upload key leaked in the public repo; removed 2026-11-25, new upload key made, waiting on Play Console upload key reset
metadata:
  type: project
---

On 2026-11-25 the old upload keystore (`docs/signing.tar.gz.enc`) and its passphrase were found in the PUBLIC repo Deathevil96/Water-Sort. Same day: the key was restored to `Signing/` on the Mac, both files were removed with git rm and pushed (they are still in git history), and a NEW upload key was made in `Signing/new/` (alias `palettepour`, cert `upload_certificate.pem`). Backup of both is in `~/Documents/PalettePour-Signing-Backup/`.

**Why:** with the passphrase sitting beside it, anyone could decrypt the old key. Play App Signing lets Google replace the upload key.

**How to apply:** release builds still sign with the OLD key (`BuildTools.ApplyReleaseSigning` reads `Signing/palettepour-upload.jks`) until the user says Google approved the upload key reset. Then move the old files to `Signing/old/` and the new files up into `Signing/`. Never commit anything in `Signing/`. See [[release-versioning-and-builds]], [[mac-handoff-2026-11-25]].
