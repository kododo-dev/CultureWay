import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import { useTheme } from '@mui/material/styles';
import { useThemeMode } from '../../context/ThemeContext.tsx';

interface PageHeaderProps {
  title: string;
  subtitle?: string;
}

const PageHeader = ({ title, subtitle }: PageHeaderProps) => {
  const theme = useTheme();
  const { mode } = useThemeMode();
  const isDark = mode === 'dark';

  return (
    <Box sx={{
      px: 3, py: 2.5,
      borderBottom: `1px solid ${isDark ? '#2e2e2e' : '#e0e0e0'}`,
      flexShrink: 0,
    }}>
      <Typography variant="h6" sx={{
        fontWeight: 700,
        fontSize: '1rem',
        color: theme.palette.text.primary,
        fontFamily: "'IBM Plex Mono', monospace",
      }}>
        {title}
      </Typography>
      {subtitle && (
        <Typography sx={{ fontSize: '0.78rem', color: theme.palette.text.secondary, mt: 0.25 }}>
          {subtitle}
        </Typography>
      )}
    </Box>
  );
};

export default PageHeader;
