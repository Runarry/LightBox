import type { FunctionalComponent } from 'preact';

export interface IconProps {
  name: 'workspaces' | 'card-view' | 'list-view' | 'logs';
  size?: number;
  className?: string;
  title?: string;
  // Allow any other SVG attributes to be passed
  [key: string]: any;
}

export const Icon: FunctionalComponent<IconProps> = ({ name, size = 16, className, title, ...rest }) => {
  const svgProps = {
    xmlns: "http://www.w3.org/2000/svg",
    width: size,
    height: size,
    fill: "currentColor",
    viewBox: "0 0 16 16",
    className,
    title,
    'aria-hidden': title ? undefined : true, // Use boolean for aria-hidden
    ...rest
  };

  switch (name) {
    case 'workspaces':
      return (
        <svg {...svgProps}>
          <path d="M1 14s1-1 4-1 4 1 4 1 1-1 4-1V2s-1 1-4 1-4-1-4-1-1 1-4 1v12zm0 0L0 13V2l1-1h14l1 1v11l-1 1H1zm13-13H2v11h12V1z"/>
          <path d="M2 3.5a.5.5 0 0 1 .5-.5H6a.5.5 0 0 1 0 1H2.5a.5.5 0 0 1-.5-.5zm0 4a.5.5 0 0 1 .5-.5H6a.5.5 0 0 1 0 1H2.5a.5.5 0 0 1-.5-.5zm0 4a.5.5 0 0 1 .5-.5H6a.5.5 0 0 1 0 1H2.5a.5.5 0 0 1-.5-.5zm4.5-.5a.5.5 0 0 0 0-1H10a.5.5 0 0 0 0 1H6.5zm0-4a.5.5 0 0 0 0-1H10a.5.5 0 0 0 0 1H6.5zm0-4a.5.5 0 0 0 0-1H10a.5.5 0 0 0 0 1H6.5z"/>
        </svg>
      );
    case 'card-view':
      return (
        <svg {...svgProps}>
          <path d="M1 2.5A1.5 1.5 0 0 1 2.5 1h3A1.5 1.5 0 0 1 7 2.5v3A1.5 1.5 0 0 1 5.5 7h-3A1.5 1.5 0 0 1 1 5.5v-3zm8 0A1.5 1.5 0 0 1 10.5 1h3A1.5 1.5 0 0 1 15 2.5v3A1.5 1.5 0 0 1 13.5 7h-3A1.5 1.5 0 0 1 9 5.5v-3zm-8 8A1.5 1.5 0 0 1 2.5 9h3A1.5 1.5 0 0 1 7 10.5v3A1.5 1.5 0 0 1 5.5 15h-3A1.5 1.5 0 0 1 1 13.5v-3zm8 0A1.5 1.5 0 0 1 10.5 9h3A1.5 1.5 0 0 1 15 10.5v3A1.5 1.5 0 0 1 13.5 15h-3A1.5 1.5 0 0 1 9 13.5v-3z"/>
        </svg>
      );
    case 'list-view':
      return (
        <svg {...svgProps}>
          <path fill-rule="evenodd" d="M2.5 12a.5.5 0 0 1 .5-.5h10a.5.5 0 0 1 0 1H3a.5.5 0 0 1-.5-.5zm0-4a.5.5 0 0 1 .5-.5h10a.5.5 0 0 1 0 1H3a.5.5 0 0 1-.5-.5zm0-4a.5.5 0 0 1 .5-.5h10a.5.5 0 0 1 0 1H3a.5.5 0 0 1-.5-.5z"/>
        </svg>
      );
    case 'logs':
      return (
        <svg {...svgProps}>
          <path d="M0 2a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2V2zm5.5 10.5a.5.5 0 0 0 .5.5h4a.5.5 0 0 0 0-1H6a.5.5 0 0 0-.5.5zm0-2a.5.5 0 0 0 .5.5h4a.5.5 0 0 0 0-1H6a.5.5 0 0 0-.5.5zm0-2a.5.5 0 0 0 .5.5h4a.5.5 0 0 0 0-1H6a.5.5 0 0 0-.5.5zm0-2a.5.5 0 0 0 .5.5h4a.5.5 0 0 0 0-1H6a.5.5 0 0 0-.5.5zm-2 4a.5.5 0 0 0 .5.5h.793l.5.5.5-.5h.793a.5.5 0 0 0 0-1H4a.5.5 0 0 0-.5.5zm0-2a.5.5 0 0 0 .5.5h.793l.5.5.5-.5h.793a.5.5 0 0 0 0-1H4a.5.5 0 0 0-.5.5zm0-2a.5.5 0 0 0 .5.5h.793l.5.5.5-.5h.793a.5.5 0 0 0 0-1H4a.5.5 0 0 0-.5.5zm0-2a.5.5 0 0 0 .5.5h.793l.5.5.5-.5h.793a.5.5 0 0 0 0-1H4a.5.5 0 0 0-.5.5z"/>
        </svg>
      );
    default:
      // Optionally return a placeholder or null for unknown icons
      console.warn(`Icon not found: ${name}`);
      return null;
  }
};