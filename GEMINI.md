# GEMINI.md — TUM Trust & Security (Antigravity & Gemini CLI)

This project enforces the ECC-adapted engineering harness for Google Antigravity and Gemini CLI.

See [AGENTS.md](./AGENTS.md) for repository architecture, core principles, and the full skills catalog.

## Antigravity Workspace Guidelines

- **Skill Discovery**: Skills are registered under `.agents/skills/`. The agent activates them based on prompt relevance and task context.
- **Rules Precedence**: The rules defined in this file and `AGENTS.md` take precedence during code generation, editing, and execution.
- **Verification Loop**: Run `dotnet build` and `dotnet test` using `run_command` whenever making structural changes.
- **Security Check**: As an Identity Verification SDK, every boundary handling tokens, biometrics, citizen IDs, or smart card PCSC frames must follow zero-trust defensive programming.

## Quick Skill Triggers

- For C# / .NET code: refer to [.agents/skills/dotnet-patterns/SKILL.md](.agents/skills/dotnet-patterns/SKILL.md)
- For testing & xUnit: refer to [.agents/skills/csharp-testing/SKILL.md](.agents/skills/csharp-testing/SKILL.md)
- For security & token review: refer to [.agents/skills/security-review/SKILL.md](.agents/skills/security-review/SKILL.md)
- For adding an Identity Provider: refer to [.agents/skills/add-identity-provider/SKILL.md](.agents/skills/add-identity-provider/SKILL.md)
- Before concluding a task: perform the verification loop in [.agents/skills/verification-loop/SKILL.md](.agents/skills/verification-loop/SKILL.md) and self-score with [.agents/skills/agent-self-evaluation/SKILL.md](.agents/skills/agent-self-evaluation/SKILL.md).
