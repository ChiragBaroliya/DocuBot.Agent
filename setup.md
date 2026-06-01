# Project Setup Instructions

## Step 1: Extract and Copy Files

1. Extract the provided folder (if in a zip or archive).
2. Copy all files and folders from the extracted folder into your local repository directory.

---

## Step 2: Set Environment Variables

Set the following user environment variables on your system:


### For Windows (Command Prompt):
```sh
setx GROQAI_API_KEY "api_key"
setx GROQAI_MODEL "llama-3.3-70b-versatile"
```

### For Windows (Control Panel - GUI):
1. Open the **Control Panel**.
2. Go to **System and Security** > **System** > **Advanced system settings**.
3. Click the **Environment Variables...** button.
4. Under **User variables**, click **New...**
5. Enter the variable name `GROQAI_API_KEY` and value `api_key`, then click **OK**.
6. Repeat to add `GROQAI_MODEL` and value `llama-3.3-70b-versatile`.
7. Click **OK** to close all dialogs.

> **⚠️ CRITICAL:** After completing Step 2 (using either Command Prompt or GUI), you **MUST RESTART** your terminal, command prompt, or IDE (like VS Code). If you do not restart, the new environment variables will not be detected and the commit hook will crash.

### For Linux/macOS (Terminal):
```sh
export GROQAI_API_KEY="api_key"
export GROQAI_MODEL=""
```

---

## Step 3: Install Dependencies and Setup Husky

1. Ensure you have Node.js and npm installed. [Download Node.js](https://nodejs.org/)
2. In your project directory, run:

```sh
npm install
npm run setup-husky
```

---

Now your environment is ready, and Husky will validate commits automatically.
