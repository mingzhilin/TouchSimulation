#ifndef __REGION_H__
#define __REGION_H__

#include <stdint.h>

typedef uint8_t	    bool;

#define TRUE	    1
#define FALSE	    0

#define FRMWIDTH    48
#define FRMHEIGHT   25

#define FRMSIZE	    (FRMWIDTH*FRMHEIGHT)

typedef struct
{
    uint8_t w;
    uint8_t h;
	int16_t * pixels;
} image_i16_t;

typedef struct {
	uint8_t x;
	uint8_t y;
} point_u8_t;

typedef union
{
	struct
	{
		point_u8_t top_left;
		point_u8_t bottom_right;
	};
	struct
	{
		uint8_t left;
		uint8_t top;
		uint8_t right;
		uint8_t bottom;
	};
} rect_t;

#define N_REGIONS_MAX		62

int algo_label8cc_and_extract_boxes(rect_t *boundingBoxes, const int16_t *src, uint16_t *dest, int threshold, const rect_t * roi);

void dump_i16_image(const char * title, const char * format, const int16_t* pImg, int width, int height);

#endif //!__REGION_H__
