import { useEffect, useState, type CSSProperties } from "react";
import type { Meta, StoryObj } from "@storybook/nextjs-vite";
import { userEvent, within } from "storybook/test";
import { TextField } from "./TextField";

const stageStyle: CSSProperties = {
  display: "flex",
  alignItems: "center",
  justifyContent: "center",
  width: "100%",
  minHeight: "72vh",
  boxSizing: "border-box",
  padding: 24,
};

const fieldFrameStyle: CSSProperties = { width: 390 };

declare global {
  interface Window {
    __textFieldTourSetStep?: (step: TextFieldTourStep) => void;
  }
}

type TextFieldTourStep = "default" | "hover" | "focused" | "error";

function TextFieldTour() {
  const [step, setStep] = useState<TextFieldTourStep>("default");

  useEffect(() => {
    window.__textFieldTourSetStep = setStep;
    return () => {
      delete window.__textFieldTourSetStep;
    };
  }, []);

  const isError = step === "error";

  return (
    <div style={stageStyle} data-testid="text-field-tour" data-tour-step={step}>
      <div style={fieldFrameStyle}>
        <TextField
          key={isError ? "error" : "normal"}
          name={isError ? "email-error" : "field"}
          label={isError ? "Email" : "Ім'я"}
          kind={isError ? "email" : "text"}
          placeholder={isError ? "name@example.com" : "Дмитро"}
          defaultValue={isError ? "not-an-email" : undefined}
          error={isError ? "Введіть дійсну електронну адресу" : undefined}
          forceHover={step === "hover"}
          autoFocus={step === "focused"}
        />
      </div>
    </div>
  );
}

const meta = {
  title: "Shared/TextField",
  component: TextField,
  tags: ["autodocs"],
  args: {
    label: "Ім'я",
    placeholder: "Дмитро",
    kind: "text",
    name: "field",
  },
  argTypes: {
    kind: {
      control: "select",
      options: ["text", "email", "password", "tel", "number", "search", "url"],
    },
  },
  parameters: {
    layout: "fullscreen",
  },
  decorators: [
    (Story, context) => {
      if (context.name === "Presentation Tour") {
        return <Story />;
      }
      return (
        <div style={stageStyle}>
          <div style={fieldFrameStyle}>
            <Story />
          </div>
        </div>
      );
    },
  ],
} satisfies Meta<typeof TextField>;

export default meta;
type Story = StoryObj<typeof meta>;

/** Звичайне поле — білий border (`--input-border-default`). */
export const Default: Story = {};

/**
 * Hover — border `primary-light-blue` (`--input-border-hover`).
 * `forceHover` тримає стан стабільно в Storybook/відео.
 */
export const Hover: Story = {
  args: {
    forceHover: true,
  },
};

/** Focus — border `primary-blue` (`--input-border-focus`). */
export const Focused: Story = {
  args: {
    autoFocus: true,
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const input = canvas.getByLabelText("Ім'я");
    await userEvent.click(input);
  },
};

/** Error — червоний border + повідомлення. */
export const Error: Story = {
  args: {
    error: "Введіть дійсну електронну адресу",
    label: "Email",
    kind: "email",
    defaultValue: "not-an-email",
    name: "email-error",
  },
};

/**
 * Один екран без зміни story URL — для запису кліпів без блимання.
 * Керування: `window.__textFieldTourSetStep('hover' | ...)`
 */
export const PresentationTour: Story = {
  render: () => <TextFieldTour />,
  parameters: {
    layout: "fullscreen",
    docs: { disable: true },
  },
};
