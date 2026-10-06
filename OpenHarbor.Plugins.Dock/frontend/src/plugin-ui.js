import React from 'react';
import ReactDOM from 'react-dom';

export const {
	Activity,
	Children,
	Component,
	Fragment,
	Profiler,
	PureComponent,
	StrictMode,
	Suspense,
	ViewTransition,
	act,
	addTransitionType,
	cache,
	cacheSignal,
	captureOwnerStack,
	cloneElement,
	createContext,
	createElement,
	createRef,
	forwardRef,
	isValidElement,
	lazy,
	memo,
	startTransition,
	unstable_useCacheRefresh,
	use,
	useActionState,
	useCallback,
	useContext,
	useDebugValue,
	useDeferredValue,
	useEffect,
	useEffectEvent,
	useId,
	useImperativeHandle,
	useInsertionEffect,
	useLayoutEffect,
	useMemo,
	useOptimistic,
	useReducer,
	useRef,
	useState,
	useSyncExternalStore,
	useTransition,
	version,
} = React;
export { React as default };
export const { createPortal, flushSync, unstable_batchedUpdates } = ReactDOM;
export { createRoot, hydrateRoot } from 'react-dom/client';
export { jsx, jsxs } from 'react/jsx-runtime';
export { jsxDEV } from 'react/jsx-dev-runtime';
export { PluginUiProvider, Button, Label, Text, Input, TextArea, Form, Layout, Checkbox, Toggle, Upload } from './semantic-components.tsx';