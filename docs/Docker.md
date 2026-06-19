# Switch Docker Build

Use the local Docker image when you need to build the Switch host directly.

```bash
docker build -t helengine-switch .
docker run --rm -v "$PWD":/workspace -w /workspace helengine-switch make
```

If Docker Desktop's credential helper blocks anonymous pulls on this machine, use:

```bash
DOCKER_CONFIG=/tmp/docker-no-creds docker build -t helengine-switch .
DOCKER_CONFIG=/tmp/docker-no-creds docker run --rm -v "$PWD":/workspace -w /workspace helengine-switch make
```

The build emits `build/helengine_switch.elf` and `build/helengine_switch.nro`.
