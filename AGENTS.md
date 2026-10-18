# Contributor requirements

Keep this repository independent of FScript.Language and FScript.Runtime.
Run `make build`, `make test`, `make pack`, `make verify-api`, and `make verify-changelog` before committing.
Builds must have zero warnings. Features and fixes require automated coverage and specification/architecture updates.
Every ordinary commit must add a concise single-line bullet under CHANGELOG.md Unreleased. Release commits may leave it empty.
Use SemVer: compatible 1.x packages keep unsigned AssemblyVersion 1.0.0.0. Public record and union shapes are frozen within a major version.
Release with `make release-prepare version=X.Y.Z`, push main with --follow-tags, wait for the tag workflow's draft, verify its changelog notes and compare link, then publish that draft. Never bypass CI draft creation.
Only NuGet publication belongs here. No product binaries, signing, npm, editor, or website releases.
