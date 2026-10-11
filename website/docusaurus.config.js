// @ts-check
// Documentation site of the Tenveo PTZ Controller, published to GitHub Pages by
// .github/workflows/docs.yml. See https://docusaurus.io/docs/api/docusaurus-config

import {themes as prismThemes} from 'prism-react-renderer';

const repoUrl = 'https://github.com/bzzdevlabs/visca-cam-app';

/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'Tenveo PTZ Controller',
  tagline: 'Drive a VISCA / UVC PTZ camera from Windows and macOS',
  favicon: 'img/favicon.ico',

  future: {
    v4: true,
  },

  url: 'https://bzzdevlabs.github.io',
  baseUrl: '/visca-cam-app/',
  organizationName: 'bzzdevlabs',
  projectName: 'visca-cam-app',
  trailingSlash: false,

  onBrokenLinks: 'throw',
  markdown: {
    hooks: {
      onBrokenMarkdownLinks: 'throw',
    },
  },

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      /** @type {import('@docusaurus/preset-classic').Options} */
      ({
        // Docs-only site: the documentation is served from the root.
        docs: {
          routeBasePath: '/',
          sidebarPath: './sidebars.js',
          editUrl: `${repoUrl}/tree/main/website/`,
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      }),
    ],
  ],

  themeConfig:
    /** @type {import('@docusaurus/preset-classic').ThemeConfig} */
    ({
      image: 'img/screenshots/screenshot-light.png',
      colorMode: {
        respectPrefersColorScheme: true,
      },
      navbar: {
        title: 'Tenveo PTZ',
        logo: {
          alt: 'Tenveo PTZ',
          src: 'img/logo.svg',
        },
        items: [
          {type: 'docSidebar', sidebarId: 'docs', position: 'left', label: 'Docs'},
          {href: `${repoUrl}/releases/latest`, label: 'Download', position: 'right'},
          {href: repoUrl, label: 'GitHub', position: 'right'},
        ],
      },
      footer: {
        style: 'dark',
        links: [
          {
            title: 'Docs',
            items: [
              {label: 'Getting started', to: '/'},
              {label: 'Troubleshooting', to: '/troubleshooting'},
              {label: 'Building from source', to: '/development/building'},
            ],
          },
          {
            title: 'Project',
            items: [
              {label: 'Releases', href: `${repoUrl}/releases`},
              {label: 'Issues', href: `${repoUrl}/issues`},
              {label: 'Source code', href: repoUrl},
            ],
          },
        ],
        copyright: `Tenveo PTZ Controller. Built with Docusaurus.`,
      },
      prism: {
        theme: prismThemes.github,
        darkTheme: prismThemes.dracula,
        additionalLanguages: ['bash', 'powershell'],
      },
    }),
};

export default config;
