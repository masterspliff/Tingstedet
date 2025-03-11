# Tingstedet

A community forum platform built with .NET 9 and Blazor WebAssembly.

## GitHub Secrets Setup for Claude AI Integration

This project uses Claude AI to generate content. The API key is securely stored in GitHub Secrets to avoid hardcoding it in the codebase.

### Setting up the Claude API key in GitHub Secrets

1. Navigate to your GitHub repository
2. Go to Settings > Secrets and variables > Actions
3. Click "New repository secret"
4. Name: `CLAUDE_API_KEY`
5. Value: Your Claude API key
6. Click "Add secret"

### How the GitHub Action workflow uses the secret

The GitHub Actions workflow in `.github/workflows/deploy.yml` automatically replaces a placeholder in the `appsettings.json` file with the actual API key during the build process:

```yaml
- name: Update appsettings
  run: |
    sed -i 's/CLAUDE_API_KEY_PLACEHOLDER/${{ secrets.CLAUDE_API_KEY }}/g' server/appsettings.json
```

## Local Development

For local development, you'll need to:

1. Create a Claude API key on the Anthropic website
2. Update the `Claude:ApiKey` value in `server/appsettings.json` with your actual API key
3. Make sure not to commit your API key to the repository

## Running the Application

```bash
# Run the server
dotnet run --project server

# Run the web app
dotnet run --project webapp
```

## Updating Content

Once the application is running, you can generate new content by clicking the "Generate Content" button on the homepage. This uses the Claude API to create random forum posts and comments.