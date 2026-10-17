import React, { createContext, useCallback, useContext, useMemo } from 'react';
import { useParams } from 'react-router-dom';
import { useShell } from '@kododo-dev/way-shell';
import { useTranslations } from './TranslationsContext.tsx';

export function extractNamespace(key: string): string {
  const i = key.lastIndexOf('.');
  return i > 0 ? key.substring(0, i) : '';
}

export interface NamespaceNode {
  /** Full dotted path from the tree root, e.g. "Admin.Users". */
  path: string;
  /** The node's own segment, e.g. "Users". */
  label: string;
  children: NamespaceNode[];
}

function buildNamespaceTree(namespaces: Iterable<string>): NamespaceNode[] {
  const roots: NamespaceNode[] = [];
  const nodeByPath = new Map<string, NamespaceNode>();

  const ensureNode = (path: string): NamespaceNode => {
    const existing = nodeByPath.get(path);
    if (existing) return existing;

    const lastDot = path.lastIndexOf('.');
    const node: NamespaceNode = {
      path,
      label: lastDot >= 0 ? path.substring(lastDot + 1) : path,
      children: [],
    };
    nodeByPath.set(path, node);

    if (lastDot >= 0) ensureNode(path.substring(0, lastDot)).children.push(node);
    else roots.push(node);

    return node;
  };

  for (const ns of namespaces) ensureNode(ns);

  const sortRecursively = (nodes: NamespaceNode[]) => {
    nodes.sort((a, b) => a.label.localeCompare(b.label));
    for (const node of nodes) sortRecursively(node.children);
  };
  sortRecursively(roots);

  return roots;
}

function flattenPaths(nodes: NamespaceNode[]): string[] {
  const paths: string[] = [];
  const walk = (list: NamespaceNode[]) => {
    for (const node of list) {
      paths.push(node.path);
      walk(node.children);
    }
  };
  walk(nodes);
  return paths;
}

interface NamespaceContextValue {
  namespaceTree: NamespaceNode[];
  /** Every path present in the tree (including synthesized intermediate nodes), flattened and sorted. */
  namespacePaths: string[];
  hasRootKeys: boolean;
  selectedNamespace: string | null;
  setSelectedNamespace: (ns: string | null) => void;
}

const NamespaceContext = createContext<NamespaceContextValue>({
  namespaceTree: [],
  namespacePaths: [],
  hasRootKeys: false,
  selectedNamespace: null,
  setSelectedNamespace: () => {},
});

const ROOT_PATH = 'root';
const NAMESPACE_PATH = 'ns/';

/** The namespace in the address, relative to the Way's root: '' all, 'root' the keys without one, 'ns/A.B' a namespace. */
function namespaceOf(path: string): string | null {
  if (path === ROOT_PATH) return '';
  if (path.startsWith(NAMESPACE_PATH)) return decodeURIComponent(path.slice(NAMESPACE_PATH.length)) || null;
  return null;
}

export const NamespaceProvider = ({ children }: { children: React.ReactNode }) => {
  const { translations } = useTranslations();
  // The selection lives in the address, so it can be linked to, goes back with the browser, and closes the shell's
  // menu on narrow screens. Paths are relative to the Way's root through the shell's navigate().
  const { navigate, wayId } = useShell();
  const selectedNamespace = namespaceOf(useParams()['*'] ?? '');
  const setSelectedNamespace = useCallback(
    (ns: string | null) =>
      navigate(wayId, ns === null ? '' : ns === '' ? ROOT_PATH : `${NAMESPACE_PATH}${encodeURIComponent(ns)}`),
    [navigate, wayId],
  );

  const { namespaceTree, namespacePaths, hasRootKeys } = useMemo(() => {
    const nsSet = new Set<string>();
    let rootKeys = false;
    for (const t of translations) {
      const ns = extractNamespace(t.key);
      if (ns) nsSet.add(ns);
      else rootKeys = true;
    }
    const tree = buildNamespaceTree(nsSet);
    return { namespaceTree: tree, namespacePaths: flattenPaths(tree), hasRootKeys: rootKeys };
  }, [translations]);

  return (
    <NamespaceContext.Provider
      value={{ namespaceTree, namespacePaths, hasRootKeys, selectedNamespace, setSelectedNamespace }}
    >
      {children}
    </NamespaceContext.Provider>
  );
};

export const useNamespace = () => useContext(NamespaceContext);
