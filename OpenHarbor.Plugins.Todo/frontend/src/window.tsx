import React, { useEffect, useState } from '/_host/plugin-ui.js';
import { DndContext, KeyboardSensor, MouseSensor, TouchSensor, useSensor, useSensors, type DragEndEvent } from '@dnd-kit/core';
import { SortableContext, arrayMove, sortableKeyboardCoordinates, useSortable, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { Check, Circle, Pencil, Plus, Save, Trash2, X } from 'lucide-react';
import '@fontsource/ibm-plex-sans/latin-400.css';
import '@fontsource/ibm-plex-sans/latin-500.css';
import '@fontsource/ibm-plex-sans/latin-600.css';
import './window.css';

import { Button, Input, Text, TextArea } from '/_host/plugin-ui.js';

type Todo = {
  id: string;
  name: string;
  description: string | null;
  isFinished: boolean;
  position: number;
  finishedUtc: string | null;
};

type TodoDraft = { name: string; description: string };

const apiUrl = new URL(/* @vite-ignore */ './api/todos', import.meta.url).toString();

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiUrl}${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  });
  if (!response.ok) {
    const body = await response.json().catch(() => null) as { error?: string } | null;
    throw new Error(body?.error ?? `Request failed (${response.status})`);
  }
  if (response.status === 204) return undefined as T;
  return await response.json() as T;
}

function ordered(items: Todo[]): Todo[] {
  return [...items].sort((left, right) => {
    if (left.isFinished !== right.isFinished) return Number(left.isFinished) - Number(right.isFinished);
    if (left.isFinished) return (left.finishedUtc ?? '').localeCompare(right.finishedUtc ?? '') || left.id.localeCompare(right.id);
    return left.position - right.position || left.id.localeCompare(right.id);
  });
}

function SortableTodo({
  todo,
  editing,
  draft,
  onDraftChange,
  onEdit,
  onSave,
  onCancel,
  onToggle,
  onDelete,
  error,
  itemStyle,
}: {
  todo: Todo;
  editing: boolean;
  draft: TodoDraft;
  onDraftChange: (draft: TodoDraft) => void;
  onEdit: () => void;
  onSave: () => void;
  onCancel: () => void;
  onToggle: () => void;
  onDelete: () => void;
  error: string;
  itemStyle: React.CSSProperties;
}) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({
    id: todo.id,
    disabled: todo.isFinished || editing,
  });
  const style = { ...itemStyle, transform: CSS.Transform.toString(transform), transition };
  const stopDragKey = (event: React.KeyboardEvent) => event.stopPropagation();
  const stopDragPointer = (event: React.PointerEvent) => event.stopPropagation();

  return (
    <article
      ref={setNodeRef}
      style={style}
      className={`todo-card${todo.isFinished ? ' is-finished' : ''}${isDragging ? ' is-dragging' : ''}`}
      {...(todo.isFinished || editing ? {} : attributes)}
      {...(todo.isFinished || editing ? {} : listeners)}
      role="listitem"
      aria-label={`${todo.name}, ${todo.isFinished ? 'finished' : 'active'} Todo`}
      aria-roledescription={todo.isFinished ? undefined : 'sortable Todo'}
      tabIndex={todo.isFinished || editing ? undefined : 0}
    >
      <button
        className="completion-button"
        type="button"
        aria-label={todo.isFinished ? `Reopen ${todo.name}` : `Finish ${todo.name}`}
        title={todo.isFinished ? 'Reopen Todo' : 'Finish Todo'}
        onClick={onToggle}
        onKeyDown={stopDragKey}
        onPointerDown={stopDragPointer}
      >
        {todo.isFinished ? <Check size={18} strokeWidth={2.5} /> : <Circle size={20} strokeWidth={1.7} />}
      </button>

      <div className="todo-copy">
        {editing ? (
          <div className="edit-fields">
            <Input
              autoFocus
              aria-label="Todo name"
              maxLength={256}
              value={draft.name}
              onChange={(event: React.ChangeEvent<HTMLInputElement>) => onDraftChange({ ...draft, name: event.target.value })}
              onKeyDown={stopDragKey}
              onPointerDown={stopDragPointer}
            />
            <TextArea
              aria-label="Todo description"
              maxLength={10000}
              rows={3}
              value={draft.description}
              onChange={(event: React.ChangeEvent<HTMLTextAreaElement>) => onDraftChange({ ...draft, description: event.target.value })}
              onKeyDown={stopDragKey}
              onPointerDown={stopDragPointer}
            />
          </div>
        ) : (
          <>
            <Text className="todo-name">{todo.name}</Text>
            {todo.description && <p className="todo-description">{todo.description}</p>}
          </>
        )}
        {error && <p className="item-error" role="alert">{error}</p>}
      </div>

      <div className="todo-actions">
        {editing ? (
          <>
            <Button type="text" aria-label="Save changes" title="Save" icon={<Save size={16} />} onClick={onSave} onPointerDown={stopDragPointer} />
            <Button type="text" aria-label="Cancel editing" title="Cancel" icon={<X size={17} />} onClick={onCancel} onPointerDown={stopDragPointer} />
          </>
        ) : (
          <Button type="text" aria-label={`Edit ${todo.name}`} title="Edit" icon={<Pencil size={15} />} onClick={onEdit} onPointerDown={stopDragPointer} />
        )}
        <Button type="text" danger aria-label={`Delete ${todo.name}`} title="Delete" icon={<Trash2 size={16} />} onClick={onDelete} onPointerDown={stopDragPointer} />
      </div>
    </article>
  );
}

export default function TodoWindow() {
  const [todos, setTodos] = useState<Todo[]>([]);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [editingId, setEditingId] = useState<string | null>(null);
  const [draft, setDraft] = useState<TodoDraft>({ name: '', description: '' });
  const [deleteTarget, setDeleteTarget] = useState<Todo | null>(null);
  const [error, setError] = useState('');
  const [itemError, setItemError] = useState('');
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  useEffect(() => {
    void request<Todo[]>('').then((items) => setTodos(ordered(items))).catch((reason: Error) => setError(reason.message)).finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!deleteTarget) return;
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setDeleteTarget(null);
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [deleteTarget]);

  const activeTodos = todos.filter((todo) => !todo.isFinished);
  const finishedTodos = todos.filter((todo) => todo.isFinished);
  const sensors = useSensors(
    useSensor(MouseSensor, { activationConstraint: { distance: 7 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 180, tolerance: 8 } }),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );

  async function createTodo(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError('');
    if (!name.trim()) {
      setError('Enter a name containing at least one non-whitespace character.');
      return;
    }
    setBusy(true);
    try {
      const created = await request<Todo>('', { method: 'POST', body: JSON.stringify({ name, description }) });
      setTodos((current) => ordered([...current, created]));
      setName('');
      setDescription('');
    } catch (reason) {
      setError((reason as Error).message);
    } finally {
      setBusy(false);
    }
  }

  function beginEdit(todo: Todo) {
    setEditingId(todo.id);
    setDraft({ name: todo.name, description: todo.description ?? '' });
    setItemError('');
  }

  async function saveEdit(todo: Todo) {
    setItemError('');
    try {
      const updated = await request<Todo>(`/${todo.id}`, { method: 'PUT', body: JSON.stringify({ name: draft.name, description: draft.description }) });
      setTodos((current) => ordered(current.map((item) => item.id === todo.id ? updated : item)));
      setEditingId(null);
    } catch (reason) {
      setItemError((reason as Error).message);
    }
  }

  async function toggleFinished(todo: Todo) {
    setError('');
    try {
      const updated = await request<Todo>(`/${todo.id}/finished`, { method: 'PUT', body: JSON.stringify({ isFinished: !todo.isFinished }) });
      setTodos((current) => ordered(current.map((item) => item.id === todo.id ? updated : item)));
    } catch (reason) {
      setError((reason as Error).message);
    }
  }

  async function finishDelete() {
    if (!deleteTarget) return;
    setError('');
    try {
      await request<void>(`/${deleteTarget.id}`, { method: 'DELETE' });
      setTodos((current) => current.filter((item) => item.id !== deleteTarget.id));
      setDeleteTarget(null);
    } catch (reason) {
      setError((reason as Error).message);
    }
  }

  async function reorder(event: DragEndEvent) {
    if (!event.over || event.active.id === event.over.id) return;
    const oldIndex = activeTodos.findIndex((todo) => todo.id === event.active.id);
    const newIndex = activeTodos.findIndex((todo) => todo.id === event.over?.id);
    if (oldIndex < 0 || newIndex < 0) return;
    const nextActive = arrayMove(activeTodos, oldIndex, newIndex);
    setError('');
    try {
      await request<void>('/order', { method: 'PUT', body: JSON.stringify({ ids: nextActive.map((todo) => todo.id) }) });
      const positions = new Map(nextActive.map((todo, index) => [todo.id, index]));
      setTodos((current) => ordered(current.map((todo) => positions.has(todo.id) ? { ...todo, position: positions.get(todo.id)! } : todo)));
    } catch (reason) {
      setError((reason as Error).message);
    }
  }

  function renderTodo(todo: Todo, index: number) {
    return (
      <SortableTodo
        key={todo.id}
        todo={todo}
        editing={editingId === todo.id}
        draft={draft}
        onDraftChange={setDraft}
        onEdit={() => beginEdit(todo)}
        onSave={() => void saveEdit(todo)}
        onCancel={() => { setEditingId(null); setItemError(''); }}
        onToggle={() => void toggleFinished(todo)}
        onDelete={() => setDeleteTarget(todo)}
        error={editingId === todo.id ? itemError : ''}
        itemStyle={{ '--item-index': index } as React.CSSProperties}
      />
    );
  }

  return (
    <main className="todo-app">
      <header className="todo-header">
        <div>
          <p className="eyebrow">ONE LIST · {activeTodos.length} ACTIVE</p>
          <h1>Todo</h1>
        </div>
        <div className="header-count" aria-label={`${finishedTodos.length} finished Todos`}>
          <Check size={14} /> <span>{finishedTodos.length}</span>
        </div>
      </header>

      <form className="composer" onSubmit={(event) => void createTodo(event)}>
        <div className="composer-fields">
          <Input
            aria-label="Todo name"
            placeholder="What needs doing?"
            maxLength={256}
            value={name}
            onChange={(event: React.ChangeEvent<HTMLInputElement>) => setName(event.target.value)}
          />
          <TextArea
            aria-label="Todo description"
            placeholder="Add a note (optional)"
            maxLength={10000}
            rows={2}
            value={description}
            onChange={(event: React.ChangeEvent<HTMLTextAreaElement>) => setDescription(event.target.value)}
          />
        </div>
        <Button type="primary" htmlType="submit" icon={<Plus size={16} />} disabled={busy} aria-label="Create Todo">
          Create
        </Button>
      </form>

      {error && <p className="app-error" role="alert">{error}</p>}
      {loading ? <p className="list-state">Loading Todos…</p> : (
        <DndContext sensors={sensors} onDragEnd={(event) => void reorder(event)}>
          <section className="todo-section" aria-labelledby="active-heading">
            <div className="section-heading">
              <h2 id="active-heading">Active</h2>
              <span>{activeTodos.length}</span>
            </div>
            {activeTodos.length ? (
              <SortableContext items={activeTodos.map((todo) => todo.id)} strategy={verticalListSortingStrategy}>
                <div className="todo-list" role="list" aria-label="Active Todos">
                  {activeTodos.map(renderTodo)}
                </div>
              </SortableContext>
            ) : <p className="list-state">Nothing active right now.</p>}
          </section>

          <section className="todo-section finished-section" aria-labelledby="finished-heading">
            <div className="section-heading">
              <h2 id="finished-heading">Finished</h2>
              <span>{finishedTodos.length}</span>
            </div>
            {finishedTodos.length ? (
              <div className="todo-list" role="list" aria-label="Finished Todos">
                {finishedTodos.map(renderTodo)}
              </div>
            ) : <p className="list-state">Finished Todos stay here.</p>}
          </section>
        </DndContext>
      )}

      {deleteTarget && (
        <div className="dialog-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) setDeleteTarget(null); }}>
          <section className="delete-dialog" role="alertdialog" aria-modal="true" aria-labelledby="delete-title" aria-describedby="delete-description">
            <div className="dialog-icon"><Trash2 size={19} /></div>
            <h2 id="delete-title">Delete this Todo?</h2>
            <p id="delete-description"><strong>{deleteTarget.name}</strong> will be permanently removed.</p>
            <div className="dialog-actions">
              <Button autoFocus onClick={() => setDeleteTarget(null)}>Cancel</Button>
              <Button danger type="primary" onClick={() => void finishDelete()}>Delete permanently</Button>
            </div>
          </section>
        </div>
      )}
    </main>
  );
}