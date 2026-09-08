"use client";

import { useEffect, useState } from "react";
import { getHealth } from "@/lib/api";

export function SystemStatus() {
  const [attempt, setAttempt] = useState(0);
  const [state, setState] = useState<{
    kind: "loading" | "success" | "error";
    message: string;
  }>({ kind: "loading", message: "Checking API and database…" });

  useEffect(() => {
    const controller = new AbortController();
    getHealth(controller.signal)
      .then(() => {
        if (!controller.signal.aborted) {
          setState({
            kind: "success",
            message: "Connected: frontend → API → PostgreSQL",
          });
        }
      })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) {
          setState({
            kind: "error",
            message:
              error instanceof TypeError
                ? "Cannot reach the API. Check that it is running and the API URL and CORS origin are correct."
                : error instanceof Error
                  ? error.message
                  : "The health check failed.",
          });
        }
      });
    return () => controller.abort();
  }, [attempt]);

  return (
    <section aria-labelledby="status-title">
      <h2 id="status-title">Development status</h2>
      <p role="status" aria-live="polite" data-state={state.kind}>
        {state.message}
      </p>
      <button
        disabled={state.kind === "loading"}
        onClick={() => {
          setState({ kind: "loading", message: "Checking API and database…" });
          setAttempt((value) => value + 1);
        }}
      >
        Check again
      </button>
    </section>
  );
}
