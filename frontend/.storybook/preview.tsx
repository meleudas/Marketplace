import type { Preview } from "@storybook/nextjs-vite";
import { INITIAL_VIEWPORTS, MINIMAL_VIEWPORTS } from "storybook/viewport";
import "@fontsource/inter/400.css";
import "@fontsource/inter/500.css";
import "@fontsource/inter/600.css";
import "@fontsource/montserrat/400.css";
import "@fontsource/montserrat/500.css";
import "@fontsource/montserrat/600.css";
import "../src/app/globals.css";

const preview: Preview = {
  parameters: {
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
    layout: "centered",
    backgrounds: {
      default: "booktop-dark",
      values: [
        { name: "booktop-dark", value: "#131417" },
        { name: "black", value: "#000000" },
      ],
    },
    viewport: {
      options: {
        ...MINIMAL_VIEWPORTS,
        ...INITIAL_VIEWPORTS,
      },
    },
    nextjs: {
      appDirectory: true,
    },
    a11y: {
      test: "todo",
    },
  },
  initialGlobals: {
    viewport: { value: "responsive", isRotated: false },
  },
  decorators: [
    (Story) => (
      <div
        style={{
          fontFamily: 'var(--font-primary), "Inter", system-ui, sans-serif',
          color: "var(--color-text)",
          minWidth: 320,
        }}
      >
        <Story />
      </div>
    ),
  ],
};

export default preview;
