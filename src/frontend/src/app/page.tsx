import { SystemStatus } from "@/components/system-status";

export default function Home() {
  return (
    <main>
      <p className="eyebrow">CARPOOL · LOCAL DEVELOPMENT</p>
      <h1>A simpler daily commute.</h1>
      <p>
        A carpooling project for commuters in Egypt. This foundation connects a
        mobile-first web app to an independent API and PostgreSQL database.
      </p>
      <SystemStatus />
      <p className="note">
        Ride sharing features are coming in later development phases.
      </p>
    </main>
  );
}
