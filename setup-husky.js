// setup-husky.js
const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

// Install husky if not installed
try {
  require.resolve('husky');
} catch (e) {
  console.log('Installing husky...');
  execSync('npm install husky --save-dev', { stdio: 'inherit' });
}


// Initialize husky if .husky doesn't exist
if (!fs.existsSync('.husky')) {
  console.log('Initializing husky...');
  execSync('npx husky init', { stdio: 'inherit' });
}

// Remove default pre-commit hook if it exists
const preCommitPath = path.join('.husky', 'pre-commit');
if (fs.existsSync(preCommitPath)) {
  console.log('Removing default pre-commit hook...');
  fs.unlinkSync(preCommitPath);
}


// --- commit-msg hook ---
const shCommitMsgPath = path.join('.husky', 'commit-msg');
if (!fs.existsSync(shCommitMsgPath)) {
  console.log('Adding commit-msg shell hook...');
  const shCommitMsgScript = `#!/bin/sh\n \ncommit_msg_file=$1\n \n"./docubot-agent/DocuBot.Agent.exe" "$commit_msg_file"\n \nif [ $? -ne 0 ]; then \n\texit 1 \nfi\n \nexit 0\n`;
  fs.writeFileSync(shCommitMsgPath, shCommitMsgScript, { mode: 0o755 });
}



console.log('Husky setup complete!');
