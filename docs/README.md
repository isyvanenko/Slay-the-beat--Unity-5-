# Slay (The beat) Documentation

This folder contains the source pages and configuration support for the repository's automated Doxygen documentation.

- Published documentation: <https://isyvanenko.github.io/Slay-the-beat--Unity-5-/>
- Generated Doxygen index after a local build: [generated/html/index.html](generated/html/index.html)
- Main documentation landing page source: [index.md](index.md)
- Doxygen group definitions: [groups.dox](groups.dox)

## GitHub Pages Setup

GitHub Pages must be configured to use **GitHub Actions** as the source. Do not use **Deploy from a branch** for this pipeline.

If the `github-pages` environment has protection rules, make sure the default branch, `main`, is allowed to deploy to it.

## Pipeline Overview

Pull requests that touch project scripts, documentation sources, the `Doxyfile`, documentation scripts, or documentation workflows run `.github/workflows/docs-pr.yml`. That workflow installs Doxygen and Graphviz, runs `Scripts/Docs/validate-doxygen.ps1`, and uploads the generated HTML as a workflow artifact.

Pushes to `main` run `.github/workflows/docs.yml`. It validates the docs the same way, adds `.nojekyll`, uploads `docs/generated/html` as a GitHub Pages artifact, and deploys it to the `github-pages` environment.

Generated output is written to `docs/generated/` and should not be committed.

## Maintainer Guidance

Add Doxygen comments to public classes, serialized fields, and important methods when changing scripts. C# XML comments work well with Doxygen:

```csharp
/// <summary>
/// Explains what this class or method is responsible for.
/// </summary>
/// <param name="value">Describe non-obvious parameters.</param>
```

Use `@ingroup` when a class belongs to one of the groups in [groups.dox](groups.dox), such as `ui`, `gameplay`, `audio`, `tools`, or `data`.

Run the local validation script before opening a pull request when Doxygen is installed:

```powershell
./Scripts/Docs/validate-doxygen.ps1
```
