import React, { Component, createRoot, PluginUiProvider, Suspense, useEffect, useMemo, useRef, useState } from '/_host/plugin-ui.js';
import { AppstoreOutlined, CloseOutlined, FullscreenExitOutlined, FullscreenOutlined, LogoutOutlined, ReloadOutlined, SearchOutlined, UserOutlined } from '@ant-design/icons';
import { Button, Checkbox, ConfigProvider, Dropdown, Form, Input, Layout, Spin, Switch, Typography, Upload } from 'antd';
import './style.css';

const sharedComponents = {
  Button,
  Label: Form.Item,
  Text: Typography.Text,
  Input,
  TextArea: Input.TextArea,
  Form,
  Layout,
  Checkbox,
  Toggle: Switch,
  Upload,
};

type LaunchMode = 'DashboardWindow' | 'NewBrowserTab' | 0 | 1;
type Application = {
  id: string;
  name: string;
  iconSvg: string;
  launchMode: LaunchMode;
  routeSubpath: string;
};
type LoadedApplication = Application & { module: React.ComponentType };

class ApplicationErrorBoundary extends Component<React.PropsWithChildren, { failed: boolean }> {
  state = { failed: false };

  static getDerivedStateFromError() {
    return { failed: true };
  }

  render() {
    if (this.state.failed) {
      return <div className="window-error"><span>Application unavailable</span><small>This app could not be loaded.</small></div>;
    }
    return this.props.children;
  }
}

function matchesName(name: string, query: string) {
  const needle = query.trim().toLocaleLowerCase();
  if (!needle) return true;
  const haystack = name.toLocaleLowerCase();
  let position = 0;
  for (const character of needle) {
    position = haystack.indexOf(character, position);
    if (position === -1) return false;
    position += 1;
  }
  return true;
}

function Dock() {
  const [applications, setApplications] = useState<Application[]>([]);
  const [catalogError, setCatalogError] = useState(false);
  const [selectorOpen, setSelectorOpen] = useState(false);
  const [query, setQuery] = useState('');
  const [selection, setSelection] = useState(0);
  const [openApps, setOpenApps] = useState<LoadedApplication[]>([]);
  const [activeId, setActiveId] = useState<string | null>(null);
  const [fullScreenId, setFullScreenId] = useState<string | null>(null);
  const searchRef = useRef<React.ElementRef<typeof Input>>(null);
  const openingApps = useRef(new Set<string>());

  function resetDashboardView() {
    openingApps.current.clear();
    setOpenApps([]);
    setActiveId(null);
    setFullScreenId(null);
  }

  useEffect(() => {
    resetDashboardView();
  }, []);

  useEffect(() => {
    void fetch('/api/dashboard/applications')
      .then((response) => {
        if (!response.ok) throw new Error('Application catalog unavailable');
        return response.json() as Promise<Application[]>;
      })
      .then(setApplications)
      .catch(() => setCatalogError(true));
  }, []);

  useEffect(() => {
    if (selectorOpen) {
      setQuery('');
      setSelection(0);
      requestAnimationFrame(() => searchRef.current?.focus());
    }
  }, [selectorOpen]);

  const launchableApps = useMemo(() => applications, [applications]);
  const filteredApps = useMemo(
    () => launchableApps.filter((application) => matchesName(application.name, query)),
    [launchableApps, query],
  );

  function closeSelector() {
    setSelectorOpen(false);
  }

  async function launch(application: Application) {
    if (application.launchMode === 'NewBrowserTab' || application.launchMode === 1) {
      window.open(`/plugins/${application.routeSubpath}/index.html`, '_blank', 'noopener,noreferrer');
      closeSelector();
      return;
    }

    const alreadyOpen = openApps.find((app) => app.id === application.id);
    if (alreadyOpen || openingApps.current.has(application.id)) {
      if (alreadyOpen) setActiveId(application.id);
      closeSelector();
      return;
    }

    openingApps.current.add(application.id);
    try {
      const moduleUrl = `/plugins/${application.routeSubpath}/window.js`;
      const loaded = await import(/* @vite-ignore */ moduleUrl) as { default?: React.ComponentType };
      if (!loaded.default) throw new Error('The application has no default component export');
      setOpenApps((current) => [...current, { ...application, module: loaded.default! }]);
      setActiveId(application.id);
    } catch {
      setOpenApps((current) => [...current, { ...application, module: () => <div className="window-error"><span>Application unavailable</span><small>This app could not be loaded.</small></div> }]);
      setActiveId(application.id);
    } finally {
      openingApps.current.delete(application.id);
      closeSelector();
    }
  }

  function onSearchKeyDown(event: React.KeyboardEvent<HTMLInputElement>) {
    if (event.key === 'Escape') {
      event.preventDefault();
      closeSelector();
    } else if (event.key === 'ArrowDown' && filteredApps.length > 0) {
      event.preventDefault();
      setSelection((current) => (current + 1) % filteredApps.length);
    } else if (event.key === 'ArrowUp' && filteredApps.length > 0) {
      event.preventDefault();
      setSelection((current) => (current - 1 + filteredApps.length) % filteredApps.length);
    } else if (event.key === 'Enter' && filteredApps[selection]) {
      event.preventDefault();
      void launch(filteredApps[selection]);
    }
  }

  function closeApp(id: string) {
    setOpenApps((current) => current.filter((app) => app.id !== id));
    setFullScreenId((current) => current === id ? null : current);
    setActiveId((current) => {
      if (current !== id) return current;
      return openApps.find((app) => app.id !== id)?.id ?? null;
    });
  }

  async function runTaskbarAction(action: 'restart' | 'logout') {
    if (action === 'restart' && !window.confirm('Restart the server now? The current process will stop and requires a supervisor to relaunch it.'))
      return;

    try {
      const tokenResponse = await fetch('/api/admin/antiforgery');
      if (!tokenResponse.ok) throw new Error('Antiforgery token unavailable');
      const { requestToken } = await tokenResponse.json() as { requestToken: string };
      const response = await fetch(`/api/admin/${action}`, {
        method: 'POST',
        headers: { RequestVerificationToken: requestToken },
      });
      if (!response.ok) throw new Error(`${action} request failed`);
      if (action === 'logout') window.location.assign('/Account/Login');
      else window.alert('Restart requested. The host must be relaunched by the configured supervisor.');
    } catch {
      window.alert(`Could not ${action} the server. Please try again.`);
    }
  }

  return (
    <ConfigProvider theme={{
      token: {
        colorPrimary: '#75c7a7',
        colorText: '#192521',
        colorBgContainer: '#fbfcfa',
        borderRadius: 6,
        fontFamily: '"DM Sans", "Segoe UI", sans-serif',
      },
    }}>
      <main className="desktop">
        <div className="desktop-mark" aria-hidden="true"><span>OH</span><i /></div>
        {openApps.map((application) => {
          const ApplicationWindow = application.module;
          const active = application.id === activeId;
          const fullScreen = application.id === fullScreenId;
          return (
            <section
              className={`app-window${active ? ' is-active' : ''}${fullScreen ? ' is-fullscreen' : ''}`}
              key={application.id}
              aria-label={`${application.name} window`}
              aria-hidden={!active}
            >
              <header className="window-titlebar">
                <span className="window-title">{application.name}</span>
                <div className="window-controls">
                  <Button
                    type="text"
                    aria-label={fullScreen ? 'Exit full screen' : 'Full screen'}
                    title={fullScreen ? 'Exit full screen' : 'Full screen'}
                    icon={fullScreen ? <FullscreenExitOutlined /> : <FullscreenOutlined />}
                    onClick={() => setFullScreenId(fullScreen ? null : application.id)}
                  />
                  <Button type="text" danger aria-label={`Close ${application.name}`} title="Close" icon={<CloseOutlined />} onClick={() => closeApp(application.id)} />
                </div>
              </header>
              <div className="window-content">
                <ApplicationErrorBoundary key={application.id}>
                  <Suspense fallback={<div className="window-loading"><Spin /></div>}>
                    <PluginUiProvider components={sharedComponents}>
                      <ApplicationWindow />
                    </PluginUiProvider>
                  </Suspense>
                </ApplicationErrorBoundary>
              </div>
            </section>
          );
        })}
        {openApps.length === 0 && <div className="desktop-empty" aria-hidden="true" />}
        {catalogError && <div className="catalog-error" role="status">Application list is unavailable.</div>}

        {selectorOpen && (
          <div className="selector" role="dialog" aria-label="Applications">
            <div className="selector-heading"><AppstoreOutlined /><span>Applications</span><kbd>ESC</kbd></div>
            <Input
              ref={searchRef}
              prefix={<SearchOutlined />}
              placeholder="Find an application"
              value={query}
              onChange={(event) => { setQuery(event.target.value); setSelection(0); }}
              onKeyDown={onSearchKeyDown}
              aria-label="Find an application"
            />
            <div className="app-list" role="listbox" aria-label="Launchable applications">
              {filteredApps.map((application, index) => (
                <button
                  className={`app-option${index === selection ? ' is-selected' : ''}`}
                  key={application.id}
                  type="button"
                  role="option"
                  aria-selected={index === selection}
                  onMouseEnter={() => setSelection(index)}
                  onClick={() => void launch(application)}
                >
                  <AppIcon application={application} />
                  <span>{application.name}</span>
                  <small>{application.launchMode === 'NewBrowserTab' || application.launchMode === 1 ? 'New tab' : 'Open'}</small>
                </button>
              ))}
              {filteredApps.length === 0 && <div className="no-results">{catalogError ? 'Could not load applications' : 'No matching applications'}</div>}
            </div>
          </div>
        )}

        <nav className="taskbar" aria-label="Taskbar">
          <Button
            className={`launcher${selectorOpen ? ' is-open' : ''}`}
            aria-label="Applications"
            title="Applications"
            icon={<AppstoreOutlined />}
            onClick={() => setSelectorOpen((current) => !current)}
          />
          <div className="taskbar-divider" />
          <div className="open-apps">
            {openApps.map((application) => (
              <button
                className={`taskbar-app${application.id === activeId ? ' is-active' : ''}`}
                key={application.id}
                type="button"
                title={application.name}
                aria-label={`Focus ${application.name}`}
                aria-current={application.id === activeId ? 'page' : undefined}
                onClick={() => setActiveId(application.id)}
              >
                <AppIcon application={application} />
                <span>{application.name}</span>
              </button>
            ))}
          </div>
          <Typography.Text className="taskbar-brand">OpenHarbor</Typography.Text>
          <Dropdown
            trigger={['click']}
            menu={{
              items: [
                { key: 'restart', icon: <ReloadOutlined />, label: 'Restart server' },
                { key: 'logout', icon: <LogoutOutlined />, label: 'Log out', danger: true },
              ],
              onClick: ({ key }) => {
                if (key === 'restart' || key === 'logout') void runTaskbarAction(key);
              },
            }}
          >
            <Button
              className="taskbar-menu"
              type="text"
              aria-label="Account and server actions"
              title="Account and server actions"
              icon={<UserOutlined />}
            />
          </Dropdown>
        </nav>
      </main>
    </ConfigProvider>
  );
}

function AppIcon({ application }: { application: Application }) {
  const [failed, setFailed] = useState(false);
  if (!application.iconSvg || failed) return <span className="fallback-icon"><AppstoreOutlined /></span>;
  const source = `data:image/svg+xml;charset=utf-8,${encodeURIComponent(application.iconSvg)}`;
  return <img className="app-icon" src={source} alt="" onError={() => setFailed(true)} />;
}

createRoot(document.getElementById('root')!).render(<Dock />);
