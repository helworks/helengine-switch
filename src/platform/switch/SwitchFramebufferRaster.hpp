#pragma once
#include <algorithm>
#include <cmath>
#include <cstdint>

namespace helengine::switch_platform {
/// Rasterizes authored parallelogram quads into a borrowed linear RGBA8888 framebuffer without frame allocations.
class SwitchFramebufferRaster {
public:
    /// Describes a sampled straight-alpha color in normalized units.
    struct Color { float R, G, B, A; };
    /// Stores screen-space geometry and interpolated texture or shape-local coordinates.
    struct Vertex { float X, Y, U, V, LocalX, LocalY; };
    /// Borrows the current framebuffer only until its presentation boundary.
    void Begin(uint32_t* pixels, int width, int height, int stride) {
        Pixels = pixels; Width = width; Height = height; Stride = stride; WrittenPixels = 0;
    }
    /// Ends the framebuffer borrow so no drawable may retain a submitted buffer.
    void End() { Pixels = nullptr; }
    /// Reports actual blended sample count for numeric guest health checks without framebuffer capture.
    uint64_t GetWrittenPixels() const { return WrittenPixels; }
    /// Samples and blends one ordered quad, honoring its scissor and rounded-shape border/corner mask.
    void Draw(const Vertex* quad, const uint8_t* texture, int textureWidth, int textureHeight,
        Color fill, Color border, float halfWidth, float halfHeight, float radius, float borderWidth,
        const float* corners, int left, int top, int right, int bottom) {
        if (Pixels == nullptr) return;
        float rx = quad[1].X - quad[0].X, ry = quad[1].Y - quad[0].Y;
        float dx = quad[3].X - quad[0].X, dy = quad[3].Y - quad[0].Y;
        float determinant = rx * dy - ry * dx;
        if (!std::isfinite(determinant) || std::abs(determinant) < 0.00001f) return;
        float minX = quad[0].X, maxX = minX, minY = quad[0].Y, maxY = minY;
        for (int i = 1; i < 4; ++i) {
            minX = std::min(minX, quad[i].X); maxX = std::max(maxX, quad[i].X);
            minY = std::min(minY, quad[i].Y); maxY = std::max(maxY, quad[i].Y);
        }
        left = std::max({0, left, static_cast<int>(std::floor(minX))});
        top = std::max({0, top, static_cast<int>(std::floor(minY))});
        right = std::min({Width, right, static_cast<int>(std::ceil(maxX))});
        bottom = std::min({Height, bottom, static_cast<int>(std::ceil(maxY))});
        for (int y = top; y < bottom; ++y) for (int x = left; x < right; ++x) {
            float px = x + 0.5f - quad[0].X, py = y + 0.5f - quad[0].Y;
            float s = (px * dy - py * dx) / determinant;
            float t = (rx * py - ry * px) / determinant;
            if (s < 0 || s >= 1 || t < 0 || t >= 1) continue;
            Color color = fill;
            if (texture != nullptr) {
                float u = quad[0].U + s * (quad[1].U - quad[0].U) + t * (quad[3].U - quad[0].U);
                float v = quad[0].V + s * (quad[1].V - quad[0].V) + t * (quad[3].V - quad[0].V);
                Color texel = Sample(texture, textureWidth, textureHeight, u, v);
                color = {fill.R * texel.R, fill.G * texel.G, fill.B * texel.B, fill.A * texel.A};
            } else {
                float lx = quad[0].LocalX + s * (quad[1].LocalX - quad[0].LocalX) + t * (quad[3].LocalX - quad[0].LocalX);
                float ly = quad[0].LocalY + s * (quad[1].LocalY - quad[0].LocalY) + t * (quad[3].LocalY - quad[0].LocalY);
                int corner = (ly < 0 ? 0 : 2) + (lx < 0 ? 0 : 1);
                float r = corners[corner] > 0 ? std::min(radius, std::min(halfWidth, halfHeight)) : 0;
                float qx = std::abs(lx) - halfWidth + r, qy = std::abs(ly) - halfHeight + r;
                float distance = std::hypot(std::max(qx, 0.0f), std::max(qy, 0.0f)) + std::min(std::max(qx, qy), 0.0f) - r;
                float coverage = std::clamp(0.5f - distance, 0.0f, 1.0f);
                float interior = borderWidth > 0 ? std::clamp(0.5f - distance - borderWidth, 0.0f, 1.0f) : 1.0f;
                color = Mix(border, fill, interior); color.A *= coverage;
            }
            if(color.A > 0) { Blend(Pixels[y * Stride + x], color); ++WrittenPixels; }
        }
    }
private:
    /// Holds the host-owned framebuffer, never freed by the renderer.
    uint32_t* Pixels = nullptr;
    /// Stores logical frame dimensions and physical row stride in pixels.
    int Width = 0, Height = 0, Stride = 0;
    /// Counts submitted nontransparent framebuffer samples in the active frame.
    uint64_t WrittenPixels = 0;
    /// Interpolates straight-alpha components for bilinear texture filtering and shape edges.
    static Color Mix(Color a, Color b, float t) {
        return {a.R + (b.R-a.R)*t, a.G + (b.G-a.G)*t, a.B + (b.B-a.B)*t, a.A + (b.A-a.A)*t};
    }
    /// Reads one wrapped texture coordinate from independently owned RGBA bytes.
    static Color Read(const uint8_t* pixels, int width, int height, int x, int y) {
        x = (x % width + width) % width; y = (y % height + height) % height;
        const uint8_t* pixel = pixels + (y * width + x) * 4;
        return {pixel[0]/255.0f, pixel[1]/255.0f, pixel[2]/255.0f, pixel[3]/255.0f};
    }
    /// Applies bilinear repeat sampling, preserving both atlas coverage and authored tiled sprites.
    static Color Sample(const uint8_t* pixels, int width, int height, float u, float v) {
        float x = (u - std::floor(u)) * width - 0.5f, y = (v - std::floor(v)) * height - 0.5f;
        int ix = static_cast<int>(std::floor(x)), iy = static_cast<int>(std::floor(y));
        float fx = x - ix, fy = y - iy;
        return Mix(Mix(Read(pixels,width,height,ix,iy), Read(pixels,width,height,ix+1,iy),fx),
            Mix(Read(pixels,width,height,ix,iy+1), Read(pixels,width,height,ix+1,iy+1),fx),fy);
    }
    /// Composites a straight-alpha source into the opaque display buffer in little-endian RGBA channel order.
    static void Blend(uint32_t& pixel, Color source) {
        float a = std::clamp(source.A, 0.0f, 1.0f);
        if (a <= 0) return;
        uint32_t result = 0xff000000u;
        const float channels[] = {source.R, source.G, source.B};
        for (int i = 0; i < 3; ++i) {
            float value = std::clamp(channels[i], 0.0f, 1.0f)*255*a + ((pixel >> (8*i)) & 255)*(1-a);
            result |= static_cast<uint32_t>(std::round(value)) << (8*i);
        }
        pixel = result;
    }
};
}
