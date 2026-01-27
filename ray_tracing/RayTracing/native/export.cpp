#include "rtweekend.h"
#include "camera.h"
#include "hittable_list.h"
#include "material.h"
#include "sphere.h"
#include <cstdint>

#define STB_IMAGE_WRITE_IMPLEMENTATION
#include "external/stb_image_write.h"

// Struktura pośrednicząca do przesyłania ustawień z C#
struct CameraConfig {
    double aspect_ratio;
    int image_width;
    int samples_per_pixel;
    int max_depth;
    double vfov;
    double lookfrom_x, lookfrom_y, lookfrom_z;
    double lookat_x, lookat_y, lookat_z;
    double vup_x, vup_y, vup_z;
    double defocus_angle;
    double focus_dist;
};

typedef void (*RenderCallback)(int samples, uint8_t* buffer);

extern "C" {
    // Zarządzanie pamięcią sceny
    hittable_list* CreateScene() { return new hittable_list(); }
    void DestroyScene(hittable_list* scene) { delete scene; }

    void AddSphere(hittable_list* scene, double cx, double cy, double cz, double r, material* mat) {
        // C# zarządza czasem życia materiału, więc używamy shared_ptr z pustym deleterem
        auto shared_mat = std::shared_ptr<material>(mat, [](material*){});
        scene->add(std::make_shared<sphere>(point3(cx, cy, cz), r, shared_mat));
    }

    // Fabryka materiałów
    material* CreateLambertian(double r, double g, double b) { return new lambertian(color(r, g, b)); }
    material* CreateMetal(double r, double g, double b, double fuzz) { return new metal(color(r, g, b), fuzz); }
    material* CreateDielectric(double ir) { return new dielectric(ir); }
    void DestroyMaterial(material* mat) { delete mat; }

    // Rendering
    void RenderScene(hittable_list* world, CameraConfig config, uint8_t* buffer, RenderCallback callback) {
        camera cam;
        cam.aspect_ratio = config.aspect_ratio;
        cam.image_width = config.image_width;
        cam.samples_per_pixel = config.samples_per_pixel;
        cam.max_depth = config.max_depth;
        cam.vfov = config.vfov;
        cam.lookfrom = point3(config.lookfrom_x, config.lookfrom_y, config.lookfrom_z);
        cam.lookat = point3(config.lookat_x, config.lookat_y, config.lookat_z);
        cam.vup = vec3(config.vup_x, config.vup_y, config.vup_z);
        cam.defocus_angle = config.defocus_angle;
        cam.focus_dist = config.focus_dist;

        cam.render(*world, buffer, callback);
    }

    void SavePng(const char* filename, int w, int h, uint8_t* buffer) {
        stbi_write_png(filename, w, h, 4, buffer, w * 4);
    }
}