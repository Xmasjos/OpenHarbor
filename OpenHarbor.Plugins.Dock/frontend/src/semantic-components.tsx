import { createContext, createElement, useContext, type ComponentType, type PropsWithChildren } from 'react';

type SemanticComponentName = 'Button' | 'Label' | 'Text' | 'Input' | 'TextArea' | 'Form' | 'Layout' | 'Checkbox' | 'Toggle' | 'Upload';
type SemanticComponents = Record<SemanticComponentName, ComponentType<any>>;

const componentImplementations = createContext<Partial<SemanticComponents>>({});

export function PluginUiProvider({
  components,
  children,
}: PropsWithChildren<{ components: SemanticComponents }>) {
  return createElement(componentImplementations.Provider, { value: components }, children);
}

function createSemanticComponent(name: SemanticComponentName) {
  return function SemanticComponent(props: Record<string, unknown>) {
    const implementation = useContext(componentImplementations)[name];
    if (!implementation) {
      throw new Error(`The dashboard has not provided the ${name} component.`);
    }
    return createElement(implementation, props);
  };
}

export const Button = createSemanticComponent('Button');
export const Label = createSemanticComponent('Label');
export const Text = createSemanticComponent('Text');
export const Input = createSemanticComponent('Input');
export const TextArea = createSemanticComponent('TextArea');
export const Form = createSemanticComponent('Form');
export const Layout = createSemanticComponent('Layout');
export const Checkbox = createSemanticComponent('Checkbox');
export const Toggle = createSemanticComponent('Toggle');
export const Upload = createSemanticComponent('Upload');