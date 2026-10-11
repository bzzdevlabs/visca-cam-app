---
sidebar_position: 4
title: Documentation
---

# Documentation

This site is built with [Docusaurus](https://docusaurus.io/) from the `website` folder and
published to GitHub Pages.

## Edit locally

Requires [Node.js](https://nodejs.org/) 20 or later.

```bash
cd website
npm ci
npm start
```

`npm start` serves the site at `http://localhost:3000/visca-cam-app/` and reloads it on every
change. Pages are Markdown files in `website/docs`; the sidebar is in `website/sidebars.js`.

Check the production build, which fails on broken links:

```bash
npm run build
```

## Publishing

`docs.yml` builds the site on every pull request that changes it, and publishes it to GitHub
Pages when the change is merged into `main`. These changes do not start the app's CI or release
workflows.

Screenshots in `website/static/img/screenshots` come from the CI runs (`ci-screenshots`
branch).
