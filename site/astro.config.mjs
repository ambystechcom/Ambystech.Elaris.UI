// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import mermaid from 'astro-mermaid';

// https://astro.build/config
export default defineConfig({
	integrations: [
		mermaid({
			mermaidConfig: {
				theme: 'dark',
			},
		}),
		starlight({
			title: 'Elaris.UI Documentation',
			description: 'A lightweight Terminal UI library for .NET with true 24-bit RGB color support',
			social: [
				{ 
					icon: 'github', 
					label: 'GitHub', 
					href: 'https://github.com/ambystechcom/Ambystech.Elaris.UI' 
				},
			],
			logo: {
				src: './src/assets/logo.png',
				alt: 'Elaris.UI Logo',
			},
			editLink: {
				baseUrl: 'https://github.com/ambystechcom/Ambystech.Elaris.UI/tree/main/lib',
			},
			sidebar: [
				{
					label: 'Introduction',
					link: '/',
				},
				{
					label: 'Installation',
					link: '/installation',
				},
				{
					label: 'Architecture',
					link: '/architecture',
				},
				{
					label: 'Widgets',
					items: [
						{ label: 'Base Widget', link: '/widgets/base-widget' },
						{
							label: 'Layout',
							items: [
								{ label: 'Container', link: '/widgets/layout/container' },
								{ label: 'Frame', link: '/widgets/layout/frame' },
								{ label: 'Panel', link: '/widgets/layout/panel' },
								{ label: 'StatusBar', link: '/widgets/layout/statusbar' },
								{ label: 'TabContainer', link: '/widgets/layout/tabcontainer' },
								{ label: 'ResponsiveContainer', link: '/widgets/layout/responsive-container' },
							],
						},
						{
							label: 'Display',
							items: [
								{ label: 'Label', link: '/widgets/display/label' },
								{ label: 'ProgressBar', link: '/widgets/display/progressbar' },
								{ label: 'TreeView', link: '/widgets/display/treeview' },
								{ label: 'TextView', link: '/widgets/display/textview' },
							],
						},
						{
							label: 'Input',
							items: [
								{ label: 'Button', link: '/widgets/input/button' },
								{ label: 'TextField', link: '/widgets/input/textfield' },
								{ label: 'AutoCompleteTextField', link: '/widgets/input/autocomplete-textfield' },
								{ label: 'Checkbox', link: '/widgets/input/checkbox' },
								{ label: 'ListBox', link: '/widgets/input/listbox' },
							],
						},
						{
							label: 'Data',
							items: [
								{ label: 'Table', link: '/widgets/data/table' },
								{ label: 'Table Columns', link: '/widgets/data/table-columns' },
								{ label: 'Specialized Columns', link: '/widgets/data/table-specialized' },
							],
						},
						{
							label: 'Menu',
							items: [
								{ label: 'MenuBar', link: '/widgets/menu/menubar' },
								{ label: 'MenuItem', link: '/widgets/menu/menuitem' },
								{ label: 'MenuDropdown', link: '/widgets/menu/menudropdown' },
							],
						},
					],
				},
				{
					label: 'Examples',
					items: [
						{ label: 'Overview', link: '/examples/' },
						{ label: 'Hello World', link: '/examples/hello-world' },
						{ label: 'Chat Demo', link: '/examples/chat-demo' },
						{ label: 'Interactive', link: '/examples/interactive' },
						{ label: 'Menu Demo', link: '/examples/menu-demo' },
						{ label: 'Table Demo', link: '/examples/table-demo' },
						{ label: 'Tabs Demo', link: '/examples/tabs-demo' },
						{ label: 'Widgets Showcase', link: '/examples/widgets' },
					],
				},
				{
					label: 'API Reference',
					items: [
						{ label: 'Overview', link: '/api-reference/' },
					],
				},
				{
					label: 'Contributing',
					items: [
						{ label: 'Guide', link: '/contributing/' },
					],
				},
			],
		}),
	]
});
