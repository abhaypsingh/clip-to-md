Below is a concise, implementationready **PRD** for a **native Windows app** built on **.NET (latest stable: .NET 9 STS)** with **WinUI 3 / Windows App SDK 1.7.x**, targeting Windows 11 and supported Windows 10. I’ve captured the flows and decisions you specified (append vs new file with a mute/autoappend override, Markdown fidelity, local Ollama integration), and added platformspecific details so engineering can move straight to design/estimation.

**References used to pin platform choices (latest as of Sep 7, 2025):**  
• **.NET 9** is the latest stable (STS); .NET 10 is in preview. [Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet)  
• **Windows App SDK 1.7.3** is the latest stable for WinUI 3 apps. [Microsoft Learn+1](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads?utm_source=chatgpt.com)  
• WinUI 3 ships with the Windows App SDK; supported on Win10 1809+ (while in support). [Microsoft Learn+2Microsoft Learn+2](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/?utm_source=chatgpt.com)  
• Clipboard change hooks should use **AddClipboardFormatListener** / **WM_CLIPBOARDUPDATE**. [Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-addclipboardformatlistener?utm_source=chatgpt.com)  
• Toast/app notifications + action buttons use **Microsoft.Windows.AppNotifications** (Windows App SDK). [Microsoft Learn+2Microsoft Learn+2](https://learn.microsoft.com/en-us/windows/apps/develop/notifications/app-notifications/app-notifications-quickstart?utm_source=chatgpt.com)  
• Local **Ollama** API: POST /api/generate (localhost:11434), supports stream:false. [Ollama](https://ollama.readthedocs.io/en/api/?utm_source=chatgpt.com)

**Product Requirements Document (PRD)**

**0. Working title**

**ClipTitle for Windows** — Autotitled, Markdownfidelity clipboard capture powered by a local LLM.

**1. Problem & goals**

Busy knowledge workers and developers constantly copy snippets from browsers, IDEs, and docs. Manually naming and organizing these snippets is tedious; formatting fidelity is often lost. **ClipTitle** watches the clipboard, derives a humanfriendly **title** with a local **Ollama** model, and stores each capture as **Markdown** (preserving structure when possible). Users can **append to the last file** or **create a new file**, with an **autoappend (mute)** mode to remove friction. All data stays local.

**Success criteria (V1)**

-   ≤ **1 s** perceived latency from copy → notification, **≤ 3 s** to persisted file on a warm system.
-   **\>95%** of clips containing HTML preserve headings/lists/links as Markdown.
-   Users can toggle **autoappend** from the **system tray** and change **save folder / model** in **Settings**.
-   App runs reliably for a full workday with **\<120 MB** RAM steadystate and **\<3% CPU** idle.

**2. Nongoals (V1)**

-   No cloud sync or remote storage.
-   No crossmachine merge/conflict resolution.
-   No image OCR. (We will store copied images as files only if added in a later milestone.)
-   No multiuser enterprise management (singleuser desktop app only).

**3. Target users**

-   **Developers & data folks**: code + logs + API payloads, want fast append mode.
-   **Researchers/writers**: preserve article formatting and links as Markdown.
-   **Power users**: want handsoff capture and tidy titles.

**4. Key use cases / user stories**

1.  **Autotitled capture to new file**  
    When I copy rich content (e.g., an article block), ClipTitle converts it to Markdown, asks “Append to last file or Create new?”, gets a concise title from Ollama, and writes a new .md with YAML front matter.
2.  **Append mode (muted prompts)**  
    I toggle **Autoappend** in the tray. Every new clip is appended under a \#\# \<Title\> section to the last file until I turn prompts back on.
3.  **Code snippet fidelity**  
    When I copy code, it’s wrapped in fenced blocks \`\`\` so formatting is preserved.
4.  **Directory/model settings**  
    I can change the save directory, Ollama model name (e.g., llama3:8b), and timeouts in a settings UI.
5.  **Fallback when LLM unavailable**  
    If Ollama is down, a reasonable title is derived from the first nonblank line.

**5. Experience overview**

**5.1 Presence & shell integration**

-   **Headless by default**: single main window (Settings), plus **tray icon** (WinUI 3 + helper lib or Win32 interop) with menu:  
    *Toggle Autoappend*, *Open Save Folder*, *Open Last File*, *Pause/Resume Capture*, *Settings*, *Exit*.  
    (Tray support via **H.NotifyIcon for WinUI** or direct Shell_NotifyIcon interop.) [NuGet](https://www.nuget.org/packages/H.NotifyIcon.WinUI/2.0.110?utm_source=chatgpt.com)[GitHub](https://github.com/HavenDV/H.NotifyIcon?utm_source=chatgpt.com)[Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyicona?utm_source=chatgpt.com)
-   **Startup** (optional): toggle “Start with Windows” (uses Run key in HKCU or a scheduled task; if MSIXpackaged, use app installer tasks).

**5.2 Capture & decision prompt**

-   On **WM_CLIPBOARDUPDATE**, the app inspects clipboard formats: try **CF_HTML** first; otherwise **CF_UNICODETEXT**. [Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-addclipboardformatlistener?utm_source=chatgpt.com)
-   If CF_HTML, parse per CF_HTML header offsets and convert **HTML → Markdown** using a vetted library (e.g., **ReverseMarkdown.Net**). [NuGet](https://www.nuget.org/packages/ReverseMarkdown/?utm_source=chatgpt.com)
-   A local **app notification** appears with **Append** / **New File** buttons; includes a oneline preview and the proposed title (if already fetched) or “Generating title…” (if still in flight). Actions use **AppNotifications** APIs. [Microsoft Learn+1](https://learn.microsoft.com/en-us/windows/apps/develop/notifications/app-notifications/app-notifications-quickstart?utm_source=chatgpt.com)

**5.3 File format & naming**

-   **New file**:  
    YYYYMMDD-HHMMSS-\<slugified-title\>.md  
    YAML front matter:
-   \---
-   title: "\<Title\>"
-   source: "clipboard"
-   created: "\<ISO8601 with offset\>"
-   \---
-   **Append**:  
    Add a section:
-   \---
-   \#\# \<Title\>
-   
-   \*Clipped:\* \<ISO8601\>
-   
-   \<content markdown\>
-   **Last file** path is persisted and used when in autoappend or when user chooses **Append**.

**5.4 Settings (WinUI 3 page)**

-   Save directory (folder picker).
-   Model config: base URL (default http://localhost:11434), model name, request timeout, title prompt template.
-   Behavior: **Ask each time** (default) vs **Autoappend**, minimum length filter, ignore patterns, duplicate detection (hash), run at startup.
-   Diagnostics: log level, “Test Ollama connection”.

**6. Detailed functional requirements**

**6.1 Clipboard monitoring**

-   **MUST** use AddClipboardFormatListener and handle WM_CLIPBOARDUPDATE to avoid polling; app must own a message loop (WinUI 3 window). [Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-addclipboardformatlistener?utm_source=chatgpt.com)
-   **Formats**:
    1.  CF_HTML (“HTML Format” per spec): extract via StartHTML/EndHTML offsets, convert to Markdown.
    2.  CF_UNICODETEXT: preserve line breaks; if heuristics detect code (e.g., braces, keywords), wrap in triple backticks.
    3.  Optional (V1.x): CF_RTF (convert to Markdown via RTF→HTML→MD pipeline).

**6.2 Title generation (LLM)**

-   Endpoint: **Ollama** POST /api/generate on localhost:11434, stream:false. **Timeout 5 s**, **retry ×1**. [Ollama](https://ollama.readthedocs.io/en/api/?utm_source=chatgpt.com)
-   Prompt template (editable):
    -   3–8 words
    -   Title Case
    -   No quotes/no trailing punctuation
    -   Code-aware titles
-   **Fallback**: first nonempty line (trim to 8 words) if LLM unavailable or empty response.

**6.3 Decision UX: append vs new**

-   **Default**: show **interactive app notification** with **Append** and **New** actions. If no last file exists and user presses **Append**, create the first file and set it as “last”. (Win App SDK AppNotifications). [Microsoft Learn](https://learn.microsoft.com/en-us/windows/apps/develop/notifications/app-notifications/app-notifications-quickstart?utm_source=chatgpt.com)
-   **Autoappend (mute)**: When enabled (via tray or Settings), **no prompt**; if there’s no last file, create one automatically.

**6.4 File I/O**

-   Writes are **atomic**: save to temp file then move.
-   **Encoding**: UTF8 with BOM off.
-   **Slugification**: [A–Z, a–z, 0–9, dash], max 80 chars.

**6.5 Privacy & safety**

-   **Localonly** by default; no telemetry.
-   **Ignore rules** (user configurable): regex patterns (e.g., \^\\d{6}\$ for OTPs), window/process name allow/deny list (V1.x; requires interop to inspect foreground window).
-   “**Pause capturing**” toggle in tray.

**6.6 Reliability**

-   **Duplicate suppression**: SHA256 hash of normalized Markdown; ignore if same as last capture.
-   **Crash safety**: singleinstance; unhandled exception handler writes crash log; restart hint in notification.

**6.7 Accessibility & localization**

-   UI contrast respects system theme.
-   Keyboard navigation for Settings.
-   All strings localizable; stored in resx.

**7. Architecture**

**7.1 Tech stack**

-   **.NET 9** (STS), **C\# 13 (compiler in VS 2022)**; target net9.0-windows10.0.19041.0 (or higher per features used). [Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet)[Microsoft Learn](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/system-requirements?utm_source=chatgpt.com)
-   **WinUI 3** via **Windows App SDK 1.7.3**. [Microsoft Learn](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads?utm_source=chatgpt.com)
-   **Windows SDK 10.0.26100** (Windows 11 24H2). [Microsoft Developer](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/?utm_source=chatgpt.com)
-   Tray icon: **H.NotifyIcon.WinUI** (preferred) or direct Shell_NotifyIcon interop. [NuGet](https://www.nuget.org/packages/H.NotifyIcon.WinUI/2.0.110?utm_source=chatgpt.com)[Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyicona?utm_source=chatgpt.com)
-   Notifications: **Microsoft.Windows.AppNotifications** builder/manager. [Microsoft Learn+1](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.windows.appnotifications?view=windows-app-sdk-1.7&utm_source=chatgpt.com)
-   HTML→Markdown: **ReverseMarkdown.Net** (with HtmlAgilityPack). [NuGet](https://www.nuget.org/packages/ReverseMarkdown/?utm_source=chatgpt.com)
-   Packaging: **MSIX** (Storeready) plus **unpackaged** support (App SDK supports app notifications in unpackaged since 1.1). [Visual Studio Magazine](https://visualstudiomagazine.com/articles/2022/06/06/windows-app-sdk-update.aspx?utm_source=chatgpt.com)

**7.2 Process model**

-   **Single desktop process** with hidden **Window** to receive clipboard messages.
-   **Clipboard Handler** → **Normalization & MD Converter** → **Title Service (Ollama client)** → **Decision Controller** (Append/New) → **File Writer** → **Notifier**.

**7.3 Data persistence**

-   **Settings**: %LOCALAPPDATA%\\ClipTitle\\settings.json
-   {
-   "saveDir": "C:\\\\Users\\\\\<user\>\\\\Documents\\\\Clips",
-   "askEveryTime": true,
-   "autoAppend": false,
-   "lastFilePath": null,
-   "ollama": { "baseUrl": "http://localhost:11434", "model": "llama3:8b", "timeoutMs": 5000 },
-   "minLength": 5,
-   "ignorePatterns": []
-   }
-   **Logs**: %LOCALAPPDATA%\\ClipTitle\\logs\\\*.log

**8. Detailed flows**

**8.1 New clip → New file**

1.  Win32 posts WM_CLIPBOARDUPDATE.
2.  App reads formats; converts to Markdown (HTML preferred).
3.  App calls Ollama with prompt → gets title (or fallback).
4.  **Notification** shows: preview + buttons **Append** / **New File**.
5.  On **New File**: create file name from timestamp + slug title → write with YAML front matter → update lastFilePath → brief “Saved” toast.

**8.2 New clip → Append (manual)**

Same as above, but user clicks **Append**. If no last file, create first file.

**8.3 Autoappend (muted)**

-   User toggles **Autoappend** in tray or Settings.
-   On every clip: if lastFilePath exists, append; else create one. No notifications (optional “quiet success” toast).

**8.4 Failure paths**

-   **Ollama timeout**: title fallback; success toast notes “(fallback)”.
-   **File write error**: error toast with “Open folder” action.
-   **Unsupported clipboard data**: info toast “Nothing to save” (optional).

**9. Performance budgets**

-   Clipboard→notification latency: **≤1 s** (median).
-   Clipboard→disk write (HTML→MD + title): **≤3 s** (median).
-   Idle CPU: **\<3%**; Working set: **\<120 MB** steadystate.
-   Robust to 100+ clips/hour.

**10. Security & privacy**

-   **Local networking only** to localhost:11434. No external calls.
-   Optin telemetry only (if ever added).
-   Obvious **Pause** control; optional “Do not capture when fullscreen app named X is active”.

**11. Telemetry (optional, optin)**

-   Counts: clips/day, append vs new ratio, conversion failures.
-   No content captured.
-   Local logs perday rolling.

**12. Accessibility**

-   Announce notifications via Narrator.
-   All actions keyboardable; access keys for UI controls.
-   High contrast themes respected.

**13. Rollout plan**

-   **M0 (Spike)**: Clipboard hook, HTML→MD, file write; manual title prompt.
-   **M1 (MVP)**: Ollama client, notifications with actions, Settings, tray, autoappend.
-   **M2 (Hardening)**: ignore rules, perf tuning, MSIX + unpackaged installer, startwithWindows.

**14. Testing strategy**

-   **Unit**: HTML→MD conversion cases, slugify, title fallback, dedupe hash.
-   **Integration**: Endtoend with a local mock of /api/generate; also with real Ollama.
-   **System**: 500 clip storm; large HTML; code blocks; long Unicode text.
-   **UX**: Notification action reliability when app window is closed. (Windows App SDK AppNotifications samples show button activation → app activation routing.) [Microsoft Learn](https://learn.microsoft.com/en-us/windows/apps/develop/notifications/app-notifications/app-notifications-quickstart?utm_source=chatgpt.com)

**15. Open questions / decisions to confirm**

-   **Packaging default**: MSIX (Storeready, identity for notifications) vs unpackaged. (App SDK supports notifications in both.) [Visual Studio Magazine](https://visualstudiomagazine.com/articles/2022/06/06/windows-app-sdk-update.aspx?utm_source=chatgpt.com)
-   **Images**: store copied images alongside .md and embed relative links? (V1.x)
-   **Process awareness**: Should we add a blocklist of source apps (e.g., password managers)?

**16. Acceptance criteria (sample)**

**AC1 Title creation**  
*Given* Ollama is running with model=llama3:8b, *when* user copies text “An overview of vector databases in 2025…”, *then* the app saves a file whose front matter title is 3–8 words in Title Case (e.g., “Vector Databases Overview”), with no quotes or trailing punctuation.

**AC2 Append vs new (prompted)**  
*Given* askEveryTime=true and a lastFilePath exists, *when* user copies and taps **Append**, *then* content is added under \#\# \<Title\> to the last file with timestamp, and no new file is created.

**AC3 Autoappend (muted)**  
*Given* askEveryTime=false & autoAppend=true, *when* user copies content, *then* it appends silently to lastFilePath (or creates the first file if needed).

**AC4 HTML fidelity**  
*Given* HTML with \<h1\>, \<ul\>, \<a\>, *when* saved, *then* headings, lists, and links are represented in Markdown per library defaults. (ReverseMarkdown.Net). [NuGet](https://www.nuget.org/packages/ReverseMarkdown/?utm_source=chatgpt.com)

**AC5 Notification actions**  
*Given* a clip is captured, *when* user clicks **New File** on the toast, *then* the app writes a new file and the toast is dismissed. (AppNotifications). [Microsoft Learn](https://learn.microsoft.com/en-us/windows/apps/develop/notifications/app-notifications/app-notifications-quickstart?utm_source=chatgpt.com)

**17. Engineering notes (for kickoff)**

-   **Project templates**: “Windows App SDK (WinUI 3 in Desktop) App” targeting **.NET 9**; install **Windows App SDK 1.7.3** NuGet. [Microsoft Learn](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads?utm_source=chatgpt.com)
-   **TFM**: net9.0-windows10.0.19041.0 (raise to 22621 if newer APIs needed). [Microsoft Learn](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/system-requirements?utm_source=chatgpt.com)
-   **Clipboard**: Create a hidden window (or the app’s main window) and call AddClipboardFormatListener(hwnd); handle WM_CLIPBOARDUPDATE; use OpenClipboard/GetClipboardData for CF_HTML/CF_UNICODETEXT. [Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-addclipboardformatlistener?utm_source=chatgpt.com)
-   **Notifications**: Use AppNotificationBuilder to add two buttons: **Append** and **New File**; route activation arguments back to a background handler. [Microsoft Learn](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.windows.appnotifications.builder.appnotificationbuilder?view=windows-app-sdk-1.7&utm_source=chatgpt.com)
-   **Tray**: Prefer **H.NotifyIcon.WinUI** to avoid WinForms dependencies; fallback to Shell_NotifyIcon if needed. [NuGet](https://www.nuget.org/packages/H.NotifyIcon.WinUI/2.0.110?utm_source=chatgpt.com)[Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shell_notifyicona?utm_source=chatgpt.com)
-   **LLM client**: HttpClient to POST /api/generate with {"model": "...", "prompt": "...", "stream": false}; 5 s timeout; 1 retry; sanitize result (strip quotes/punctuation, Title Case). [Ollama](https://ollama.readthedocs.io/en/api/?utm_source=chatgpt.com)

**Appendix A — Platform snapshot (for reviewers)**

-   **.NET 9.0.8** latest (STS) as of Aug 5 2025; **.NET 10 preview 7** as of Aug 12 2025; **.NET 8** remains LTS. [Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet)
-   **Windows App SDK 1.7.3** stable (July 2025). [Microsoft Learn](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/stable-channel?utm_source=chatgpt.com)

If you want, I can follow this PRD with a **.NET solution skeleton** (WinUI 3 + AppNotifications + Clipboard listener + ReverseMarkdown + Ollama client) that compiles and demonstrates the full endtoend flow.
