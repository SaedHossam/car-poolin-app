export type HealthResponse = {
  status: string;
  checks?: Record<string, string>;
};

const apiUrl = process.env.NEXT_PUBLIC_API_URL;
if (!apiUrl) {
  throw new Error("NEXT_PUBLIC_API_URL must be configured.");
}

export async function getHealth(signal?: AbortSignal): Promise<HealthResponse> {
  const response = await fetch(
    `${apiUrl!.replace(/\/$/, "")}/api/health/ready`,
    {
      cache: "no-store",
      signal: signal
        ? AbortSignal.any([signal, AbortSignal.timeout(10000)])
        : AbortSignal.timeout(10000),
    },
  );
  if (!response.ok) {
    throw new Error(
      response.status === 503
        ? "The API is reachable, but the database is not ready."
        : `Health check failed (HTTP ${response.status}).`,
    );
  }
  const data: unknown = await response.json();
  if (
    !data ||
    typeof data !== "object" ||
    !("status" in data) ||
    data.status !== "Healthy"
  ) {
    throw new Error("The API returned an unexpected health response.");
  }
  return data as HealthResponse;
}
