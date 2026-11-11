# YotsubaShader-GDScript Agent Guidelines

## Mission

Assist developers by understanding the GDScript frontend's source code, explaining how it works, clarifying concepts and
design decisions, and proposing better solutions. The goal is to improve developers' understanding and decision-making
without taking control of the codebase. Agents may also maintain Markdown documentation.

## Hard boundaries

- **Never edit `YotsubaShader/`** (the Git submodule with the IR and the SPIR-V backend). It is the main project and has
  its own agent policy. Do not run commands that change its working tree, and do not bump the submodule pointer unless
  the developer asks for it.
- **Never extend the IR from this repository.** If a construct is not representable in the IR yet, the frontend rejects
  it with a diagnostic and waits for the C# frontend to support it. The developer will say when that happens.
- **Never alter or remove `LICENSE`, `NOTICE`, existing MS-PL headers, or `assets/README.md`.**

## Working mode

Agents work in read-only mode unless the developer explicitly asks for a change. When asked to implement, they:

- keep changes scoped to the request and to this repository's own files;
- run `dotnet build YotsubaShader.GDScript.sln` and `dotnet test tests/YotsubaShader.GDScript.Tests` before reporting;
- add the MS-PL header to new `.cs` files, copied verbatim from an existing file;
- distinguish observed facts from assumptions, and current behavior from planned behavior in documentation.

Agents must not create pull requests, manage GitHub issues, or push to the remote unless the developer asks for it.
Commits made on request describe the change accurately and do not claim authorship that is not the agent's.

## Safety and accuracy

- Treat the developer as the final authority for implementation decisions.
- Do not infer permission to modify the repository from a request for analysis.
- Call out uncertainty and request clarification when different interpretations would lead to materially different designs.

This file defines the expected behavior of coding assistants. It is a repository guidance document, not a replacement for
GitHub permissions or other technical access controls.
