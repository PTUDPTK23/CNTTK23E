import { useCallback, useEffect, useRef, useState } from 'react';
import { errMsg } from '../utils/format';

/** Tải dữ liệu bất đồng bộ với trạng thái loading/error và reload (silent = không bật loading, dùng cho polling). */
export function useAsync<T>(fn: () => Promise<T>, deps: unknown[]) {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const fnRef = useRef(fn);
  fnRef.current = fn;
  const seq = useRef(0);

  const run = useCallback((silent: boolean) => {
    const id = ++seq.current;
    if (!silent) setLoading(true);
    fnRef.current()
      .then((d) => { if (id === seq.current) { setData(d); setError(null); setLoading(false); } })
      .catch((e) => { if (id === seq.current) { setError(errMsg(e)); setLoading(false); } });
  }, []);

  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(() => { run(false); return () => { seq.current++; }; }, deps);

  return { data, loading, error, reload: () => run(false), refresh: () => run(true) };
}
