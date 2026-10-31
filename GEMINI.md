# GEMINI.md — TUM Trust & Security (Antigravity & Gemini CLI)

This project enforces the ECC-adapted engineering harness for Google Antigravity and Gemini CLI.

See [AGENTS.md](./AGENTS.md) for repository architecture, core principles, and the full skills catalog.

## Antigravity Workspace Guidelines

- **Autonomous Orca Jira & GitHub Issues Synchronization (MANDATORY)**:
  - On **EVERY SINGLE TASK, PROMPT, FEATURE, BUG FIX, OR SPRINT DELIVERY**, the agent MUST proactively invoke the `orca` CLI to search, create, update, or transition Jira tickets, and synchronize corresponding **GitHub Issues** on the affected repositories (`gh issue create/edit/close`).
  - **The CEO NEVER needs to manually request Orca, Jira, or GitHub Issue operations.** It is a built-in default reflex of the agent.
  - Jira site: `z-3mP_i47N7xAgZ9psDZoJll` (`https://daywakler.atlassian.net`), Project: `SCRUM`.
- **Skill Discovery**: Skills are registered under `.agents/skills/`. The agent activates them automatically based on context.
- **Rules Precedence**: The rules defined in this file and `AGENTS.md` take absolute precedence during code generation, editing, and execution.
- **Verification Loop**: Run `dotnet build` and `dotnet test` using `run_command` whenever making structural changes.
- **Security Check**: As an Identity Verification SDK, every boundary handling tokens, biometrics, citizen IDs, or smart card PCSC frames must follow zero-trust defensive programming.

## Quick Skill Triggers

- **For Jira & Sprint Management (ALWAYS ACTIVE)**: refer to [.agents/skills/orca-jira-automation/SKILL.md](.agents/skills/orca-jira-automation/SKILL.md)
- For C# / .NET code: refer to [.agents/skills/dotnet-patterns/SKILL.md](.agents/skills/dotnet-patterns/SKILL.md)
- For testing & xUnit: refer to [.agents/skills/csharp-testing/SKILL.md](.agents/skills/csharp-testing/SKILL.md)
- For security & token review: refer to [.agents/skills/security-review/SKILL.md](.agents/skills/security-review/SKILL.md)
- For adding an Identity Provider: refer to [.agents/skills/add-identity-provider/SKILL.md](.agents/skills/add-identity-provider/SKILL.md)
- Before concluding a task: perform the verification loop in [.agents/skills/verification-loop/SKILL.md](.agents/skills/verification-loop/SKILL.md) and self-score with [.agents/skills/agent-self-evaluation/SKILL.md](.agents/skills/agent-self-evaluation/SKILL.md).
