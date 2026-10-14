# 🔵 Setup Guide: Using Subagents with Google Gemini

This guide explains how to use the TUM Global Subagents with **Google Gemini Gems (gemini.google.com)**, **Google AI Studio**, and the **Gemini Developer API**.

---

## 1. Creating Custom Gemini Gems (gemini.google.com)

Gemini Gems allow you to create specialized, persistent AI assistants powered by Gemini 1.5 Pro / Flash.

1. Open **Gemini** (https://gemini.google.com) and click **Gem Manager** -> **New Gem**.
2. Fill in the Gem parameters:
   - **Name:** e.g. `TUM DX & SDK Ergonomics Lead` or `TUM Frontier Tech Researcher`
   - **Instructions:** Open the corresponding markdown file in `.subagents/` (e.g. `.subagents/04-dx-architecture/dx-lead-sdk-ergonomics.md`), scroll to `## 📋 System Instructions (Copy-Paste Ready)`, copy the instructions, and paste them into the box.
3. Test with the Gem:
   - `How do we structure our C# NuGet package so a developer can initialize ThaID in 2 lines of code?`
   - `Evaluate whether we should support Post-Quantum ML-KEM in our 2027 roadmap.`
4. Click **Save**. The Gem is now permanently pinned to your left sidebar.

---

## 2. Using with Google AI Studio (System Instructions)

Google AI Studio allows deep testing with large context windows (up to 2M tokens) and temperature control:

1. Go to [Google AI Studio](https://aistudio.google.com).
2. Choose **Gemini 1.5 Pro** or **Gemini 2.0 Flash**.
3. In the **System Instructions** sidebar, paste the agent markdown prompt.
4. Under **Tools**, enable *Google Search* if you are using research agents (`legacy-retro-researcher`, `current-sota-researcher`, `frontier-future-researcher`).
5. Upload your codebase or documentation files directly into the context window.
6. Run queries and save the prompt as a reusable preset.

---

## 3. Using with Google GenAI SDK (Node.js & Python)

### Python Example:
```python
from google import genai
import pathlib

client = genai.Client()
system_instruction = pathlib.Path(".subagents/04-dx-architecture/dx-lead-sdk-ergonomics.md").read_text(encoding="utf-8")

response = client.models.generate_content(
    model='gemini-2.5-flash',
    contents='Review our C# DependencyInjection extension method for SDK initialization.',
    config=dict(
        system_instruction=system_instruction,
        temperature=0.2,
    )
)

print(response.text)
```
