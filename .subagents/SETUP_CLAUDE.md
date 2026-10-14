# 🟣 Setup Guide: Using Subagents with Claude

This guide explains how to use the TUM Global Subagents with **Claude Code (CLI)**, **Claude Projects (Web)**, and **Claude Desktop / API**.

---

## 1. Using with Claude Code (CLI)

Claude Code allows invoking subagents via natural language or system prompt flags.

### Method A: Direct Command Prompting
You can invoke any subagent by loading its markdown file directly into your session prompt:

```bash
# Example: Invoke CTO agent
claude -p "Act according to .subagents/01-executive-strategy/cto.md: Review our multi-region SDK expansion strategy."

# Example: Invoke DX Lead for SDK ergonomics
claude -p "Act according to .subagents/04-dx-architecture/dx-lead-sdk-ergonomics.md: Audit our current public API for onboarding friction."

# Example: Invoke QA Chaos Tester
claude -p "Act according to .subagents/06-hardware-quality/qa-automation-chaos-tester.md: Design a test plan for mid-transaction card ejection."
```

### Method B: Claude Code Custom Skills (`.claude/skills/`)
You can wrap subagents as Claude Code skills. When you run `/agent-name`, Claude will load the role automatically:
1. Copy the subagent file into `.claude/skills/<agent-id>/SKILL.md`
2. Example folder: `.claude/skills/dx-lead/SKILL.md`
3. In terminal, type: `/dx-lead Optimize our C# ThaID onboarding sample`

---

## 2. Using with Claude Projects (Web - claude.ai)

If your organization uses Claude Pro or Claude Team:

1. Create a new **Project** named `TUM Global Identity SDK`.
2. In **Project Knowledge**, upload:
   - The entire `.subagents/` folder (or specific agent markdown files you need).
   - Core project documentation (`README.md`, `ARCHITECTURE.md`, `ROADMAP_SECRET_MANAGEMENT.md`).
3. In **Custom Instructions (Project Prompt)**, paste the following selector instruction:

```markdown
You are part of the TUM Global Identity & AI Kiosk Ecosystem multi-agent team.
When the user prefixes a prompt with `@<agent-id>` (e.g. `@cto`, `@dx-lead`, `@legacy-researcher`, `@red-team`), you MUST strictly adopt the persona, technical constraints, standard operating procedures, and output formats defined in the corresponding file in `.subagents/`.
If no prefix is provided, act as the Software Architect and coordinate with the relevant subagents.
```

4. Usage in chat:
   - `@dx-lead How do we design an error code that tells developers their redirect URI is misconfigured?`
   - `@legacy-researcher Why did early USB smart card readers fail when kiosks were powered on?`
   - `@red-team How could an attacker spoof facial verification with a printed latex mask?`

---

## 3. Using with Anthropic API / Python / TypeScript SDK

When building programmatic multi-agent orchestrators:

```typescript
import Anthropic from '@anthropic-ai/sdk';
import * as fs from 'fs';

const anthropic = new Anthropic();
const agentPrompt = fs.readFileSync('.subagents/04-dx-architecture/dx-lead-sdk-ergonomics.md', 'utf-8');

const response = await anthropic.messages.create({
  model: 'claude-3-7-sonnet-20250219',
  max_tokens: 4096,
  system: agentPrompt,
  messages: [{ role: 'user', content: 'Review our current C# SDK public API naming.' }],
});

console.log(response.content[0].text);
```
