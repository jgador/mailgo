import React, { useEffect, useRef, useState } from 'react';

interface RichTextEditorProps {
  value: string;
  onChange: (html: string) => void;
  disabled?: boolean;
}

const FONT_SIZES = [
  { label: 'Small', value: '2' },
  { label: 'Normal', value: '3' },
  { label: 'Large', value: '4' },
  { label: 'Larger', value: '5' },
];

const FONT_FAMILIES = [
  { label: 'Sans Serif', value: 'Arial, Helvetica, sans-serif', weight: 'normal' },
  { label: 'Serif', value: 'Times New Roman, Times, serif', weight: 'normal' },
  { label: 'Fixed Width', value: '"Courier New", monospace', weight: 'normal' },
  { label: 'Wide', value: '"Arial Black", Gadget, sans-serif', weight: 'bold' },
  { label: 'Narrow', value: '"Arial Narrow", sans-serif', weight: 'normal' },
  { label: 'Comic Sans MS', value: '"Comic Sans MS", cursive, sans-serif', weight: 'normal' },
  { label: 'Garamond', value: 'Garamond, serif', weight: 'normal' },
  { label: 'Georgia', value: 'Georgia, serif', weight: 'normal' },
  { label: 'Tahoma', value: 'Tahoma, Geneva, sans-serif', weight: 'normal' },
  { label: 'Trebuchet MS', value: '"Trebuchet MS", sans-serif', weight: 'normal' },
  { label: 'Verdana', value: 'Verdana, Geneva, sans-serif', weight: 'normal' },
];
const COLORS = ['#000000', '#4B5563', '#9CA3AF', '#EF4444', '#F59E0B', '#10B981', '#3B82F6', '#8B5CF6'];
const COLOR_POPOVER_WIDTH = 140; // approximate width of the 4-column swatch grid

const RichTextEditor: React.FC<RichTextEditorProps> = ({ value, onChange, disabled }) => {
  const editorRef = useRef<HTMLDivElement>(null);
  const toolbarRef = useRef<HTMLDivElement>(null);
  const colorButtonRef = useRef<HTMLButtonElement>(null);
  const colorPopoverRef = useRef<HTMLDivElement>(null);
  const [showColorPicker, setShowColorPicker] = useState(false);
  const [colorPopoverPos, setColorPopoverPos] = useState<{ left: number; top: number }>({ left: 0, top: 0 });

  const exec = (command: string, arg?: string) => {
    document.execCommand(command, false, arg);
    editorRef.current?.focus();
  };

  const handleInput = () => {
    const html = editorRef.current?.innerHTML ?? '';
    onChange(html);
  };

  useEffect(() => {
    if (editorRef.current && editorRef.current.innerHTML !== value) {
      editorRef.current.innerHTML = value || '';
    }
  }, [value]);

  const toggleColorPicker = () => {
    if (disabled) return;
    if (showColorPicker) {
      setShowColorPicker(false);
      return;
    }
    const btnRect = colorButtonRef.current?.getBoundingClientRect();
    const toolbarRect = toolbarRef.current?.getBoundingClientRect();
    if (btnRect && toolbarRect) {
      const left = btnRect.left - toolbarRect.left + btnRect.width / 2 - COLOR_POPOVER_WIDTH / 2;
      setColorPopoverPos({
        left: Math.max(0, left),
        top: btnRect.bottom - toolbarRect.top + 4,
      });
    }
    setShowColorPicker(true);
  };

  useEffect(() => {
    if (!showColorPicker) return;
    const handleClickOutside = (e: MouseEvent) => {
      const target = e.target as Node;
      if (
        colorPopoverRef.current?.contains(target) ||
        colorButtonRef.current?.contains(target)
      ) {
        return;
      }
      setShowColorPicker(false);
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [showColorPicker]);

  return (
    <div className="space-y-2">
      <div
        ref={toolbarRef}
        className="relative flex items-center gap-1 bg-gray-50 border border-gray-200 rounded-full shadow-sm px-2 py-1"
      >
        {/* History */}
        <button
          type="button"
          onClick={() => exec('undo')}
          className="w-9 h-9 flex items-center justify-center rounded hover:bg-gray-100 text-gray-700"
          aria-label="Undo"
          disabled={disabled}
          title="Undo"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M9 5 4 9.5 9 14" />
            <path d="M20 18a8 8 0 0 0-8-8H4" />
          </svg>
        </button>
        <button
          type="button"
          onClick={() => exec('redo')}
          className="w-9 h-9 flex items-center justify-center rounded hover:bg-gray-100 text-gray-700"
          aria-label="Redo"
          disabled={disabled}
          title="Redo"
        >
          <svg xmlns="http://www.w3.org/2000/svg" className="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M15 5 20 9.5 15 14" />
            <path d="M4 18a8 8 0 0 1 8-8h8" />
          </svg>
        </button>
        <div className="w-px h-6 bg-gray-200 mx-1" />

        {/* Typography */}
        <select
          className="h-9 px-2 rounded border border-transparent bg-transparent text-sm text-gray-800 focus:outline-none hover:bg-gray-100"
          defaultValue={FONT_FAMILIES[0].value}
          onChange={(e) => exec('fontName', e.target.value)}
          disabled={disabled}
          title="Font family"
        >
          {FONT_FAMILIES.map((f) => (
            <option key={f.label} value={f.value} style={{ fontFamily: f.value, fontWeight: f.weight }}>
              {f.label}
            </option>
          ))}
        </select>
        <select
          className="h-9 px-2 rounded border border-transparent bg-transparent text-sm text-gray-800 focus:outline-none hover:bg-gray-100"
          defaultValue="3"
          onChange={(e) => exec('fontSize', e.target.value)}
          disabled={disabled}
          title="Font size"
        >
          {FONT_SIZES.map((s) => (
            <option key={s.value} value={s.value}>
              {s.label}
            </option>
          ))}
        </select>
        <div className="w-px h-6 bg-gray-200 mx-1" />

        {/* Basic styles */}
        <button type="button" onClick={() => exec('bold')} className="w-9 h-9 flex items-center justify-center rounded hover:bg-gray-100 text-gray-700 font-semibold" aria-label="Bold" disabled={disabled} title="Bold">
          B
        </button>
        <button type="button" onClick={() => exec('italic')} className="w-9 h-9 flex items-center justify-center rounded hover:bg-gray-100 text-gray-700 italic" aria-label="Italic" disabled={disabled} title="Italic">
          I
        </button>
        <button type="button" onClick={() => exec('underline')} className="w-9 h-9 flex items-center justify-center rounded hover:bg-gray-100 text-gray-700 underline" aria-label="Underline" disabled={disabled} title="Underline">
          U
        </button>
        <button
          type="button"
          onClick={toggleColorPicker}
          className="w-9 h-9 flex items-center justify-center rounded hover:bg-gray-100 text-gray-700 font-semibold relative"
          aria-label="Text color"
          disabled={disabled}
          title="Text color"
          ref={colorButtonRef}
        >
          A
          <span className="absolute left-1 right-1 -bottom-1 h-0.5 bg-gray-700" />
        </button>
        <div className="w-px h-6 bg-gray-200 mx-1" />

        {/* Paragraph formatting */}
        <button type="button" onClick={() => exec('justifyLeft')} className="w-9 h-9 flex items-center justify-center rounded hover:bg-gray-100 text-gray-700" aria-label="Align left" disabled={disabled} title="Align left">
          <svg xmlns="http://www.w3.org/2000/svg" className="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M4 6h16" />
            <path d="M4 10h10" />
            <path d="M4 14h16" />
            <path d="M4 18h10" />
          </svg>
        </button>
        <button type="button" onClick={() => exec('justifyCenter')} className="w-9 h-9 flex items-center justify-center rounded hover:bg-gray-100 text-gray-700" aria-label="Align center" disabled={disabled} title="Align center">
          <svg xmlns="http://www.w3.org/2000/svg" className="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M6 6h12" />
            <path d="M4 10h16" />
            <path d="M6 14h12" />
            <path d="M4 18h16" />
          </svg>
        </button>
        <button type="button" onClick={() => exec('justifyRight')} className="w-9 h-9 flex items-center justify-center rounded hover:bg-gray-100 text-gray-700" aria-label="Align right" disabled={disabled} title="Align right">
          <svg xmlns="http://www.w3.org/2000/svg" className="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M4 6h16" />
            <path d="M10 10h10" />
            <path d="M4 14h16" />
            <path d="M10 18h10" />
          </svg>
        </button>
        <div className="w-px h-6 bg-gray-200 mx-1" />

        {/* Utility */}
        <button type="button" onClick={() => exec('strikeThrough')} className="w-9 h-9 flex items-center justify-center rounded hover:bg-gray-100 text-gray-700" aria-label="Strikethrough" disabled={disabled} title="Strikethrough">
          <span className="line-through">S</span>
        </button>
        <button type="button" onClick={() => exec('removeFormat')} className="w-9 h-9 flex items-center justify-center rounded hover:bg-gray-100 text-gray-700" aria-label="Clear formatting" disabled={disabled} title="Clear formatting">
          <span className="relative">
            T
            <span className="absolute inset-x-0 top-1/2 h-0.5 bg-gray-700 rotate-12 origin-center" />
          </span>
        </button>
      </div>

      <div className="relative">
        <div
          ref={editorRef}
          contentEditable={!disabled}
          onInput={handleInput}
          className="w-full min-h-[260px] rounded-lg border border-gray-200 bg-white shadow-sm px-4 py-3 focus:outline-none focus:ring-2 focus:ring-brand-blue"
          data-placeholder="Compose your message..."
          suppressContentEditableWarning
          style={{ whiteSpace: 'pre-wrap' }}
        />
        {!value && (
          <div className="pointer-events-none text-gray-400 absolute top-3 left-4 select-none">
            Compose your message...
          </div>
        )}

        {showColorPicker && (
          <div
            className="absolute z-20 bg-white border border-gray-200 rounded-md shadow-lg p-2 grid grid-cols-4 gap-2"
            style={{ left: colorPopoverPos.left, top: colorPopoverPos.top }}
            ref={colorPopoverRef}
          >
            {COLORS.map((c) => (
              <button
                key={c}
                type="button"
                className="w-7 h-7 rounded border border-gray-200"
                style={{ backgroundColor: c }}
                aria-label={`Set text color ${c}`}
                onClick={() => {
                  exec('foreColor', c);
                  setShowColorPicker(false);
                }}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default RichTextEditor;
