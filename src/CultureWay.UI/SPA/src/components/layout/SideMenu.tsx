import { useState } from 'react';
import List from '@mui/material/List';
import ListItem from '@mui/material/ListItem';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import Collapse from '@mui/material/Collapse';
import IconButton from '@mui/material/IconButton';
import Divider from '@mui/material/Divider';
import Typography from '@mui/material/Typography';
import Box from '@mui/material/Box';
import LayersIcon from '@mui/icons-material/Layers';
import FolderOpenIcon from '@mui/icons-material/FolderOpen';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import { useTheme } from '@mui/material/styles';
import { useI18n } from '../../i18n/I18nContext.tsx';
import { useNamespace, type NamespaceNode } from '../../context/NamespaceContext.tsx';

const NamespaceTreeItem = ({ node, depth, onNavigate }: {
  node: NamespaceNode;
  depth: number;
  onNavigate?: () => void;
}) => {
  const theme = useTheme();
  const isDark = theme.palette.mode === 'dark';
  const { selectedNamespace, setSelectedNamespace } = useNamespace();
  const [expanded, setExpanded] = useState(true);

  const active = selectedNamespace === node.path;
  const hasChildren = node.children.length > 0;

  const iconSx = { color: isDark ? '#666' : '#aaa', minWidth: 26, '& svg': { fontSize: 16 } };
  const activeIconSx = { ...iconSx, color: theme.palette.primary.main };
  const textSx = {
    '& .MuiListItemText-primary': {
      fontSize: '0.8rem',
      fontFamily: "'IBM Plex Mono', monospace",
      color: active ? theme.palette.text.primary : theme.palette.text.secondary,
      fontWeight: active ? 600 : 400,
    },
  };

  return (
    <>
      <ListItem disablePadding>
        <ListItemButton
          selected={active}
          onClick={() => { setSelectedNamespace(node.path); onNavigate?.(); }}
          sx={{ pl: 2 + depth * 1.75 }}
        >
          {hasChildren ? (
            <IconButton
              size="small"
              onClick={e => { e.stopPropagation(); setExpanded(x => !x); }}
              sx={{ p: 0.25, mr: 0.25, color: isDark ? '#666' : '#aaa' }}
            >
              {expanded ? <ExpandMoreIcon sx={{ fontSize: 16 }} /> : <ChevronRightIcon sx={{ fontSize: 16 }} />}
            </IconButton>
          ) : (
            <Box sx={{ width: 24, flexShrink: 0 }} />
          )}
          <ListItemIcon sx={active ? activeIconSx : iconSx}><FolderOpenIcon /></ListItemIcon>
          <ListItemText primary={node.label} sx={textSx} />
        </ListItemButton>
      </ListItem>
      {hasChildren && (
        <Collapse in={expanded} timeout="auto" unmountOnExit>
          {node.children.map(child => (
            <NamespaceTreeItem key={child.path} node={child} depth={depth + 1} onNavigate={onNavigate} />
          ))}
        </Collapse>
      )}
    </>
  );
};

const SideMenu = ({ onNavigate }: { onNavigate?: () => void }) => {
  const { t } = useI18n();
  const theme = useTheme();
  const isDark = theme.palette.mode === 'dark';
  const { namespaceTree, hasRootKeys, selectedNamespace, setSelectedNamespace } = useNamespace();

  const isAll = selectedNamespace === null;
  const borderColor = isDark ? '#2e2e2e' : '#e0e0e0';

  const iconSx = { color: isDark ? '#666' : '#aaa', minWidth: 32, '& svg': { fontSize: 16 } };
  const activeIconSx = { ...iconSx, color: theme.palette.primary.main };

  const textSx = (active: boolean) => ({
    '& .MuiListItemText-primary': {
      fontSize: '0.8rem',
      fontFamily: "'IBM Plex Mono', monospace",
      color: active ? theme.palette.text.primary : theme.palette.text.secondary,
      fontWeight: active ? 600 : 400,
    },
  });

  const navigate = (ns: string | null) => {
    setSelectedNamespace(ns);
    onNavigate?.();
  };

  const showNamespaces = namespaceTree.length > 0 || hasRootKeys;

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, overflow: 'hidden' }}>
      <Box sx={{ flexShrink: 0, py: 1 }}>
        <List dense disablePadding>
          <ListItem disablePadding>
            <ListItemButton selected={isAll} onClick={() => navigate(null)}>
              <ListItemIcon sx={isAll ? activeIconSx : iconSx}><LayersIcon /></ListItemIcon>
              <ListItemText primary={t.allNamespaces} sx={textSx(isAll)} />
            </ListItemButton>
          </ListItem>
        </List>
      </Box>

      {showNamespaces && (
        <Box sx={{ display: 'flex', flexDirection: 'column', flex: 1, minHeight: 0, overflow: 'hidden' }}>
          <Divider sx={{ borderColor, mx: 2, flexShrink: 0 }} />

          <Typography sx={{
            px: 2.5, pt: 1, pb: 0.5,
            fontSize: '0.62rem',
            color: isDark ? '#444' : '#bbb',
            textTransform: 'uppercase',
            letterSpacing: '0.1em',
            fontFamily: 'monospace',
            flexShrink: 0,
          }}>
            {t.namespaces}
          </Typography>

          <Box sx={{ flex: 1, minHeight: 0, overflowY: 'auto', pb: 1 }}>
            <List dense disablePadding>
              {hasRootKeys && (
                <ListItem disablePadding>
                  <ListItemButton
                    selected={selectedNamespace === ''}
                    onClick={() => navigate('')}
                  >
                    <ListItemIcon sx={selectedNamespace === '' ? activeIconSx : iconSx}>
                      <FolderOpenIcon />
                    </ListItemIcon>
                    <ListItemText primary={t.rootNamespace} sx={textSx(selectedNamespace === '')} />
                  </ListItemButton>
                </ListItem>
              )}
              {namespaceTree.map(node => (
                <NamespaceTreeItem key={node.path} node={node} depth={0} onNavigate={onNavigate} />
              ))}
            </List>
          </Box>
        </Box>
      )}
    </Box>
  );
};

export default SideMenu;
