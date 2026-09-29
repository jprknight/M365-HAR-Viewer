# Release process

M365 Trace Analyzer uses semantic versioning and a tag-triggered GitHub Actions
workflow to build and publish Windows releases.

## When to publish a release

Publish a new release when a merged change affects the shipped executable,
runtime dependencies, security behavior, user-visible functionality, supported
trace behavior, or release packaging.

A new product release is normally unnecessary for documentation-only or
test-only changes.

## Prepare the version

Update the application version in:

```text
src\M365Trace.Web\M365Trace.Web.csproj
```

Keep these values aligned:

```xml
<Version>X.Y.Z</Version>
<AssemblyVersion>X.Y.Z.0</AssemblyVersion>
<FileVersion>X.Y.Z.0</FileVersion>
```

Create:

```text
docs\vX.Y.Z-release-notes.md
```

Release notes should summarize user-visible capabilities, security or privacy
changes, compatibility changes, and important operational behavior.

## Validate

Run:

```powershell
dotnet restore .\M365-Trace-Analyzer.sln
dotnet format .\M365-Trace-Analyzer.sln --verify-no-changes --no-restore
dotnet build .\M365-Trace-Analyzer.sln --configuration Release --no-restore -warnaserror
dotnet test .\M365-Trace-Analyzer.sln --configuration Release --no-build
.\eng\Test-ReleaseTag.ps1 -Tag "refs/tags/vX.Y.Z"
```

The CI workflow also publishes and smoke-tests the self-contained Windows
package and runs the packaged Chromium workflow.

## Commit and tag

Commit the version and release notes, push the target branch, then create and
push an annotated tag:

```powershell
git tag -a vX.Y.Z -m "M365 Trace Analyzer X.Y.Z"
git push origin vX.Y.Z
```

The release workflow verifies that the tag matches the project version.

## Release workflow

The tag-triggered workflow:

1. Verifies the release tag and project version.
2. Restores, builds, and tests the solution.
3. Publishes a self-contained Windows x64 application.
4. Configures anonymous usage telemetry when the encrypted repository
   connection-string secret is available.
5. Smoke-tests the published application.
6. Runs the packaged Chromium investigation workflow.
7. Creates the versioned ZIP archive.
8. Generates an SPDX 2.2 SBOM from the final publish directory.
9. Calculates and publishes SHA-256 values for the ZIP and SBOM.
10. Generates build-provenance and SBOM attestations.
11. Publishes the ZIP and SPDX SBOM using the matching release-notes file.
12. Removes Windows ZIP assets older than the latest two stable releases.

If telemetry configuration is absent, the published package keeps telemetry
disabled and does not show telemetry consent controls.

## Verify the release

Confirm that:

- The workflow completed successfully.
- The release is neither a draft nor a prerelease unless intended.
- The expected Windows ZIP is attached.
- The versioned SPDX SBOM is attached.
- The ZIP and SBOM checksums are available.
- Build-provenance and SBOM attestations are available.
- The packaged application displays the released version.
- The in-application update check recognizes the release.

The release workflow uses Microsoft SBOM Tool `4.1.5`. The generation script
pins and verifies the tool's SHA-256 value before execution.

The latest stable GitHub Release is the application's update source of truth.
Tags without a published release are not offered to users.

## Release ZIP retention

The current stable release (`N`) and its immediate stable predecessor (`N-1`)
retain downloadable Windows ZIP assets. Older release pages, tags, notes,
SBOMs, checksums, and attestations remain available as historical evidence,
but their Windows ZIP assets are removed.

Drafts and prereleases do not count toward the two retained stable releases.
The cleanup matches only the versioned
`M365-Trace-Analyzer-vX.Y.Z-win-x64.zip` asset and does not remove unrelated
assets.

The release workflow applies the policy immediately after publishing. The
`Release asset retention` workflow also reconciles the policy every Monday and
supports manual dry runs.

If a retained release has a security defect, run the retention workflow
manually with `dry_run` disabled and list the affected `X.Y.Z` value in
`withdraw_versions`. An explicit withdrawal takes precedence over the normal
two-release retention window.

The predecessor is retained only as a short-term rollback option. The latest
stable release remains the supported version unless `SECURITY.md` states
otherwise.
