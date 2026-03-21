import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import App from './App';

describe('App workspace interactions', () => {
  it('keeps the selected directory after switching to the error log and back', async () => {
    const user = userEvent.setup();

    render(<App />);

    await user.click(screen.getByRole('treeitem', { name: /users/i }));

    expect(screen.getByText(/Current directory: C:\\Users/i)).toBeInTheDocument();
    expect(screen.getByRole('row', { name: /administrator/i })).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /show errors/i }));
    await user.click(screen.getByRole('button', { name: /show workspace/i }));

    expect(screen.getByText(/Current directory: C:\\Users/i)).toBeInTheDocument();
    expect(screen.getByRole('row', { name: /administrator/i })).toBeInTheDocument();
  });

  it('keeps directory context and only changes focus when a file row is clicked', async () => {
    const user = userEvent.setup();

    render(<App />);

    const fileRow = screen.getByRole('row', { name: /pagefile\.sys/i });
    await user.click(fileRow);

    expect(screen.getByText(/Current directory: C:\\$/i)).toBeInTheDocument();
    expect(fileRow).toHaveAttribute('aria-selected', 'true');
  });
});
