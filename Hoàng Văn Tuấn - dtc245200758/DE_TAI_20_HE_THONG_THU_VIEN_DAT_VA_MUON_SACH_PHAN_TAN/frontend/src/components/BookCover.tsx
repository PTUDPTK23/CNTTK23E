import { useState } from 'react';

function hue(s: string): number {
  let h = 0;
  for (const c of s) h = (h * 31 + c.charCodeAt(0)) % 360;
  return h;
}

/** Ảnh bìa; nếu không có/lỗi tải thì vẽ bìa sách từ tên (không phụ thuộc ảnh bên ngoài). */
export function BookCover({ title, author, src, className = '' }: { title: string; author?: string; src?: string | null; className?: string }) {
  const [failed, setFailed] = useState(false);
  if (src && !failed) return <img className={`cover ${className}`} src={src} alt={title} loading="lazy" onError={() => setFailed(true)} />;
  const h = hue(title);
  return (
    <div className={`cover cover-fallback ${className}`}
      style={{ background: `linear-gradient(145deg, hsl(${h} 58% 42%), hsl(${(h + 45) % 360} 60% 24%))` }}>
      <span className="cover-spine" />
      <span className="cover-title">{title}</span>
      {author && <span className="cover-author">{author}</span>}
    </div>
  );
}
