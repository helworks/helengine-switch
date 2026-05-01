FROM devkitpro/devkita64:latest

ENV DEVKITPRO=/opt/devkitpro
ENV DEVKITA64=/opt/devkitpro/devkitA64
ENV PATH=/opt/devkitpro/devkitA64/bin:/opt/devkitpro/tools/bin:${PATH}

WORKDIR /workspace
