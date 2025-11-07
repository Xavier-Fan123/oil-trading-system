import React, { useState } from 'react';
import {
  Box,
  Button,
  Card,
  CardContent,
  FormControl,
  FormControlLabel,
  FormLabel,
  RadioGroup,
  Radio,
  TextField,
  Grid,
  Typography,
  Switch,
  Alert,
  CircularProgress,
  Divider,
  Select,
  MenuItem,
  InputLabel,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material';
import {
  Delete as DeleteIcon,
  Edit as EditIcon,
  Add as AddIcon,
} from '@mui/icons-material';
import { ReportSchedule, ScheduleFrequency, SCHEDULE_FREQUENCY_LABELS } from '@/types/advancedReporting';
import { advancedReportingApi } from '@/services/advancedReportingApi';

interface ReportSchedulerProps {
  reportConfigId: string;
  onScheduleSaved?: (schedule: ReportSchedule) => void;
  onCancel?: () => void;
}

interface DaySelectState {
  dayOfWeek?: number;
  dayOfMonth?: number;
}

export const ReportScheduler: React.FC<ReportSchedulerProps> = ({
  reportConfigId,
  onScheduleSaved,
  onCancel,
}) => {
  const [schedules, setSchedules] = useState<ReportSchedule[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [editingScheduleId, setEditingScheduleId] = useState<string | null>(null);
  const [showNewDialog, setShowNewDialog] = useState(false);

  // Form state
  const [form, setForm] = useState<ReportSchedule>({
    reportConfigId,
    enabled: true,
    frequency: ScheduleFrequency.Weekly,
    time: '09:00',
    timezone: 'UTC',
  });

  const [daySelection, setDaySelection] = useState<DaySelectState>({
    dayOfWeek: 1, // Monday
    dayOfMonth: 1,
  });

  // Load schedules on mount
  React.useEffect(() => {
    loadSchedules();
  }, [reportConfigId]);

  const loadSchedules = async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await advancedReportingApi.listReportSchedules(reportConfigId);
      setSchedules(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load schedules');
    } finally {
      setLoading(false);
    }
  };

  const handleSaveSchedule = async () => {
    // Validation
    if (!form.time) {
      setError('Please select a time');
      return;
    }

    try {
      setLoading(true);
      setError(null);

      let scheduleToSave = { ...form };

      // Set day based on frequency
      if (form.frequency === ScheduleFrequency.Weekly) {
        scheduleToSave.dayOfWeek = daySelection.dayOfWeek;
      } else if (form.frequency === ScheduleFrequency.Monthly) {
        scheduleToSave.dayOfMonth = daySelection.dayOfMonth;
      }

      let saved: ReportSchedule;

      if (editingScheduleId) {
        scheduleToSave.id = editingScheduleId;
        saved = await advancedReportingApi.updateReportSchedule(
          editingScheduleId,
          scheduleToSave
        );
      } else {
        saved = await advancedReportingApi.createReportSchedule(scheduleToSave);
      }

      // Update local state
      if (editingScheduleId) {
        setSchedules((prev) =>
          prev.map((s) => (s.id === editingScheduleId ? saved : s))
        );
      } else {
        setSchedules((prev) => [...prev, saved]);
      }

      // Reset form
      setShowNewDialog(false);
      setEditingScheduleId(null);
      setForm({
        reportConfigId,
        enabled: true,
        frequency: ScheduleFrequency.Weekly,
        time: '09:00',
        timezone: 'UTC',
      });
      setDaySelection({ dayOfWeek: 1, dayOfMonth: 1 });

      if (onScheduleSaved) {
        onScheduleSaved(saved);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save schedule');
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteSchedule = async (scheduleId: string) => {
    if (!window.confirm('Delete this schedule?')) return;

    try {
      setLoading(true);
      await advancedReportingApi.deleteReportSchedule(scheduleId);
      setSchedules((prev) => prev.filter((s) => s.id !== scheduleId));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete schedule');
    } finally {
      setLoading(false);
    }
  };

  const handleToggleSchedule = async (schedule: ReportSchedule) => {
    try {
      setLoading(true);
      const updated = await advancedReportingApi.toggleReportSchedule(
        schedule.id!,
        !schedule.enabled
      );
      setSchedules((prev) =>
        prev.map((s) => (s.id === schedule.id ? updated : s))
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to toggle schedule');
    } finally {
      setLoading(false);
    }
  };

  const handleEditSchedule = (schedule: ReportSchedule) => {
    setForm(schedule);
    setDaySelection({
      dayOfWeek: schedule.dayOfWeek || 1,
      dayOfMonth: schedule.dayOfMonth || 1,
    });
    setEditingScheduleId(schedule.id || null);
    setShowNewDialog(true);
  };

  const getFrequencyDescription = (schedule: ReportSchedule): string => {
    const freq = SCHEDULE_FREQUENCY_LABELS[schedule.frequency];
    const time = schedule.time;

    if (schedule.frequency === ScheduleFrequency.Weekly && schedule.dayOfWeek !== undefined) {
      const days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
      return `${freq} on ${days[schedule.dayOfWeek]} at ${time}`;
    } else if (
      schedule.frequency === ScheduleFrequency.Monthly &&
      schedule.dayOfMonth !== undefined
    ) {
      return `${freq} on day ${schedule.dayOfMonth} at ${time}`;
    }
    return `${freq} at ${time}`;
  };

  const DAYS_OF_WEEK = [
    { value: 0, label: 'Sunday' },
    { value: 1, label: 'Monday' },
    { value: 2, label: 'Tuesday' },
    { value: 3, label: 'Wednesday' },
    { value: 4, label: 'Thursday' },
    { value: 5, label: 'Friday' },
    { value: 6, label: 'Saturday' },
  ];

  const DAYS_OF_MONTH = Array.from({ length: 31 }, (_, i) => ({
    value: i + 1,
    label: (i + 1).toString(),
  }));

  return (
    <Card>
      <CardContent>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h6">Report Schedules</Typography>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => {
              setEditingScheduleId(null);
              setForm({
                reportConfigId,
                enabled: true,
                frequency: ScheduleFrequency.Weekly,
                time: '09:00',
                timezone: 'UTC',
              });
              setShowNewDialog(true);
            }}
            disabled={loading}
          >
            Add Schedule
          </Button>
        </Box>

        {error && (
          <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>
            {error}
          </Alert>
        )}

        {loading && <CircularProgress sx={{ mb: 2 }} />}

        {/* Schedules Table */}
        {schedules.length > 0 ? (
          <TableContainer component={Paper}>
            <Table size="small">
              <TableHead>
                <TableRow sx={{ backgroundColor: 'background.default' }}>
                  <TableCell width={50}>Enabled</TableCell>
                  <TableCell>Frequency</TableCell>
                  <TableCell>Next Run</TableCell>
                  <TableCell width={100}>Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {schedules.map((schedule) => (
                  <TableRow key={schedule.id}>
                    <TableCell>
                      <Switch
                        checked={schedule.enabled}
                        onChange={() => handleToggleSchedule(schedule)}
                        disabled={loading}
                      />
                    </TableCell>
                    <TableCell>{getFrequencyDescription(schedule)}</TableCell>
                    <TableCell>
                      {schedule.nextRunDate
                        ? new Date(schedule.nextRunDate).toLocaleDateString()
                        : 'Not scheduled'}
                    </TableCell>
                    <TableCell>
                      <IconButton
                        size="small"
                        onClick={() => handleEditSchedule(schedule)}
                        disabled={loading}
                      >
                        <EditIcon fontSize="small" />
                      </IconButton>
                      <IconButton
                        size="small"
                        color="error"
                        onClick={() => handleDeleteSchedule(schedule.id!)}
                        disabled={loading}
                      >
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        ) : (
          <Typography color="textSecondary" sx={{ textAlign: 'center', py: 3 }}>
            No schedules created yet
          </Typography>
        )}

        {onCancel && (
          <Box sx={{ mt: 2, display: 'flex', justifyContent: 'flex-end' }}>
            <Button onClick={onCancel}>Done</Button>
          </Box>
        )}
      </CardContent>

      {/* New/Edit Schedule Dialog */}
      <Dialog open={showNewDialog} onClose={() => setShowNewDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>
          {editingScheduleId ? 'Edit Schedule' : 'Create New Schedule'}
        </DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 2 }}>
            <FormControl fullWidth>
              <InputLabel>Frequency</InputLabel>
              <Select
                value={form.frequency}
                onChange={(e) =>
                  setForm((prev) => ({
                    ...prev,
                    frequency: e.target.value as ScheduleFrequency,
                  }))
                }
                label="Frequency"
              >
                {Object.entries(SCHEDULE_FREQUENCY_LABELS).map(([key, label]) => (
                  <MenuItem key={key} value={key}>
                    {label}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            {/* Day Selection */}
            {form.frequency === ScheduleFrequency.Weekly && (
              <FormControl fullWidth>
                <InputLabel>Day of Week</InputLabel>
                <Select
                  value={daySelection.dayOfWeek || 1}
                  onChange={(e) =>
                    setDaySelection((prev) => ({
                      ...prev,
                      dayOfWeek: e.target.value as number,
                    }))
                  }
                  label="Day of Week"
                >
                  {DAYS_OF_WEEK.map((day) => (
                    <MenuItem key={day.value} value={day.value}>
                      {day.label}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            )}

            {form.frequency === ScheduleFrequency.Monthly && (
              <FormControl fullWidth>
                <InputLabel>Day of Month</InputLabel>
                <Select
                  value={daySelection.dayOfMonth || 1}
                  onChange={(e) =>
                    setDaySelection((prev) => ({
                      ...prev,
                      dayOfMonth: e.target.value as number,
                    }))
                  }
                  label="Day of Month"
                >
                  {DAYS_OF_MONTH.map((day) => (
                    <MenuItem key={day.value} value={day.value}>
                      {day.label}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            )}

            <TextField
              label="Time"
              type="time"
              value={form.time || '09:00'}
              onChange={(e) => setForm((prev) => ({ ...prev, time: e.target.value }))}
              InputLabelProps={{ shrink: true }}
            />

            <FormControl fullWidth>
              <InputLabel>Timezone</InputLabel>
              <Select
                value={form.timezone || 'UTC'}
                onChange={(e) => setForm((prev) => ({ ...prev, timezone: e.target.value }))}
                label="Timezone"
              >
                <MenuItem value="UTC">UTC</MenuItem>
                <MenuItem value="EST">EST (UTC-5)</MenuItem>
                <MenuItem value="CST">CST (UTC-6)</MenuItem>
                <MenuItem value="MST">MST (UTC-7)</MenuItem>
                <MenuItem value="PST">PST (UTC-8)</MenuItem>
              </Select>
            </FormControl>

            <FormControlLabel
              control={
                <Switch
                  checked={form.enabled}
                  onChange={(e) =>
                    setForm((prev) => ({ ...prev, enabled: e.target.checked }))
                  }
                />
              }
              label="Enable this schedule"
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setShowNewDialog(false)}>Cancel</Button>
          <Button variant="contained" onClick={handleSaveSchedule} disabled={loading}>
            {loading ? <CircularProgress size={20} /> : 'Save Schedule'}
          </Button>
        </DialogActions>
      </Dialog>
    </Card>
  );
};

export default ReportScheduler;
