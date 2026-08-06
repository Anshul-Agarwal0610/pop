export function ConnectionStatus({connected}:{connected:boolean}) { return <span role="status" className="text-sm text-muted-foreground">{connected?"Live":"Reconnecting…"}</span> }
