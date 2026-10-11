// @ts-check

/** @type {import('@docusaurus/plugin-content-docs').SidebarsConfig} */
const sidebars = {
  docs: [
    'intro',
    'install',
    {
      type: 'category',
      label: 'Using the app',
      collapsed: false,
      items: ['usage/connect', 'usage/controls', 'usage/presets', 'usage/updates', 'usage/settings'],
    },
    'troubleshooting',
    {
      type: 'category',
      label: 'Development',
      items: ['development/building', 'development/architecture', 'development/ci', 'development/docs'],
    },
  ],
};

export default sidebars;
