# How to Upload Santa Video Generator to GitHub

## Method 1: Using Git Command Line (Recommended)

### Step 1: Initialize Git Repository
```powershell
# Navigate to project directory
cd C:\Users\luespino\source\repos\SantaVideo

# Initialize git (if not already done)
git init
```

### Step 2: Create GitHub Repository
1. Go to https://github.com/new
2. Repository name: `SantaVideo` (or your choice)
3. Description: "AI-powered Santa video generator using Azure AI Foundry Sora model"
4. Choose Public or Private
5. **DO NOT** initialize with README (we already have one)
6. Click "Create repository"

### Step 3: Add Remote and Push
```powershell
# Add all files to staging
git add .

# Commit files
git commit -m "Initial commit: Santa Video Generator with Azure Sora"

# Add GitHub remote (replace YOUR_USERNAME with your GitHub username)
git remote add origin https://github.com/YOUR_USERNAME/SantaVideo.git

# Push to GitHub
git push -u origin main
```

If you get an error about "master" vs "main", use:
```powershell
git branch -M main
git push -u origin main
```

### Step 4: Verify
Visit `https://github.com/YOUR_USERNAME/SantaVideo` to see your repo!

---

## Method 2: Using GitHub Desktop

### Step 1: Install GitHub Desktop
Download from: https://desktop.github.com/

### Step 2: Add Repository
1. Open GitHub Desktop
2. File → Add Local Repository
3. Choose: `C:\Users\luespino\source\repos\SantaVideo`
4. Click "Add Repository"

### Step 3: Publish
1. Click "Publish repository" button
2. Name: SantaVideo
3. Description: AI-powered Santa video generator
4. Choose Public/Private
5. Uncheck "Keep this code private" if you want it public
6. Click "Publish Repository"

Done! 🎉

---

## Method 3: Using Visual Studio

### If you have Visual Studio open:
1. Right-click solution in Solution Explorer
2. Select "Add Solution to Source Control"
3. Choose "Git"
4. Click "Create and Push" button
5. Sign in to GitHub
6. Configure repository name and settings
7. Click "Create and Push"

---

## Important: Verify .gitignore

Before pushing, ensure these files are **NOT** uploaded (they contain secrets):

❌ `appsettings.json` (contains API keys)
❌ `azure-config.json` (contains secrets)
❌ `bin/` and `obj/` folders
❌ Generated videos (`*.mp4`)

✅ These are already in `.gitignore` - you're protected!

Users will need to run `provision-sora-model.ps1` to generate their own config.

---

## Authentication Methods

### Option A: Personal Access Token (PAT)
1. GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Generate new token
3. Select scopes: `repo` (full control)
4. Copy token (save it somewhere safe!)
5. Use token as password when prompted during `git push`

### Option B: GitHub CLI
```powershell
# Install GitHub CLI
winget install --id GitHub.cli

# Authenticate
gh auth login

# Push code
git push
```

### Option C: SSH Key
```powershell
# Generate SSH key
ssh-keygen -t ed25519 -C "your_email@example.com"

# Copy public key
Get-Content ~/.ssh/id_ed25519.pub | clip

# Add to GitHub: Settings → SSH and GPG keys → New SSH key
# Paste the key, save

# Use SSH remote URL instead
git remote set-url origin git@github.com:YOUR_USERNAME/SantaVideo.git
git push
```

---

## Quick Command Reference

```powershell
# Check status
git status

# See what will be committed
git status --short

# View commit history
git log --oneline

# Check remote URL
git remote -v

# Update remote URL if needed
git remote set-url origin https://github.com/YOUR_USERNAME/SantaVideo.git

# Force push (use carefully!)
git push -f origin main
```

---

## Troubleshooting

### "fatal: not a git repository"
```powershell
git init
```

### "failed to push some refs"
```powershell
git pull origin main --rebase
git push origin main
```

### "Authentication failed"
- Use Personal Access Token instead of password
- Or use GitHub CLI: `gh auth login`

### "remote origin already exists"
```powershell
git remote remove origin
git remote add origin https://github.com/YOUR_USERNAME/SantaVideo.git
```

---

## After Uploading

### Add Repository Topics (optional)
On GitHub repo page, click ⚙️ next to "About":
- Topics: `csharp`, `dotnet`, `azure`, `openai`, `sora`, `video-generation`, `ai`, `christmas`

### Enable GitHub Pages (optional)
Settings → Pages → Deploy from main branch `/docs` folder

### Add License
Click "Add file" → "Create new file"
- Name: `LICENSE`
- Choose MIT License template

### Add Repository Description
Click ⚙️ next to "About":
- Description: "🎅 Generate magical Christmas videos with Santa using Azure AI Foundry's Sora model"
- Website: Your demo URL (if any)
- Topics: azure, ai, video-generation, sora, dotnet

---

## Example Full Workflow

```powershell
cd C:\Users\luespino\source\repos\SantaVideo

# Initialize and commit
git init
git add .
git commit -m "Initial commit: Santa Video Generator with Azure Sora"

# Create repo on GitHub first, then:
git remote add origin https://github.com/YOUR_USERNAME/SantaVideo.git
git branch -M main
git push -u origin main
```

🎉 Your code is now on GitHub!

### Clone command for others:
```bash
git clone https://github.com/YOUR_USERNAME/SantaVideo.git
```
