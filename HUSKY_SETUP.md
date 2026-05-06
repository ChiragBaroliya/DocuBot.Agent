# Husky & DocuBot Integration

To enable automatic branch and commit message validation for all developers:

1. Ensure you have Node.js and npm installed.
2. Run:
   ```sh
   npm install
   npm run setup-husky
   ```
3. Now, every commit will be validated by DocuBot.Agent via Husky's `commit-msg` hook.

You can share this setup with your team. Each developer just needs to run the above commands after cloning the repo.
