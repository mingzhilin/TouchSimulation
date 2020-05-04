#ifdef _MSC_VER
#define _CRT_SECURE_NO_WARNINGS
#endif

#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <string.h>
#include <ctype.h>
#include "region.h"

void line_preprocessing(char * line)
{
	int i;
	char * p = line;

	// clear line comment
	while (*p != '\0')
	{
		if (*p == '#')
		{
			*p = '\0';
			break;
		}
		p++;
	}

	// trim tails
	i = strlen(line) - 1;
	while (i >= 0 && isspace(line[i]))
	{
		line[i] = '\0';
		i--;
	}
}

int read_image(const char * filename, image_i16_t * image)
{
	char line_buffer[1024];
	int w;
	int h;
	FILE * fp = fopen(filename, "rt");
	int i;

	image->w = 0;
	image->h = 0;
	if (image == NULL || image->pixels == NULL)
	{
	    fprintf(stderr, "null image_t pointer\n");
	    return -1;
	}

	if (fp == NULL)
	{
		fprintf(stderr, "Unable to read %s.\n", filename);
		return -2;
	}

	i = 0;
	h = 0;
	while (fgets(line_buffer, sizeof(line_buffer), fp) != NULL)
	{
		char * ptr = line_buffer;
		line_preprocessing(ptr);
		if (*ptr == '\0')	// skip empty lines
			continue;
		w = 0;
		char * s = strtok(ptr, ", \n\r\t");
		while (s != NULL)
		{
			image->pixels[i++] = atol(s);
			w++;
			s = strtok(NULL, ", \n\r\t");
		}
		if (image->w == 0)
			image->w = w;
		h++;
	}
	image->h = h;
	fclose(fp);
	return 0;
}

int paste_image(image_i16_t * dest, const image_i16_t * src, unsigned offset_x, unsigned offset_y)
{
	if (src == NULL || dest == NULL)
		return -1;
	if (src->w + offset_x > dest->w || src->h + offset_y > dest->h)
		return -2;

	int y;
	for (y = 0; y< src->h; y++)
	{
		int16_t * p_dest_line = &(dest->pixels[(y + offset_y) * dest->w + offset_x]);
		int16_t * p_src_line = &(src->pixels[y * src->w]);
		memcpy(p_dest_line, p_src_line, sizeof(int16_t) * src->w);
	}
	return 0;
}

FILE * fp_log = NULL;

void dump_i16_image(const char * title, const char * format, const int16_t* pImg, int width, int height)
{
	int c,r;

	if (fp_log != NULL)
	{
		if (title == NULL)
			title = "Untitled";
		fprintf(fp_log, "%s (w=%d,h=%d)\n", title, width, height);
		for (r = 0; r < height; r++)
		{
			for (c = 0; c < width; c++)
			{
				fprintf(fp_log, format, *pImg++);
			}
			fprintf(fp_log, "\n");
		}
	}
}
/**************************************************************************
 *                                                                        
 *  Function Name:                                                        
 *                                                                        
 *  Purposes:                                                             
 *                                                                        
 *  Descriptions:                                                         
 *                                                                        
 *  Arguments:                                                            
 *                                                                        
 *  Returns:                                                              
 *                                                                        
 *  See also:                                                             
 *                                                                        
 **************************************************************************/
//int16_t imgPrc[FRMSIZE];
int16_t image_buffer[FRMSIZE];
int16_t frame_buffer[FRMSIZE];
uint16_t labeled[FRMSIZE];	// label image pixel can be uint8_t to save space.
rect_t regions[N_REGIONS_MAX];

int main_entry(char* path, int rg_threshold)
{
	int rowSize = FRMHEIGHT,colSize = FRMWIDTH;
	//int rg_threshold = 8;
	
	fp_log = fopen("region.txt", "wt");

	if (fp_log == NULL)
	{
		fprintf(stderr, "Warning: Log file open fails\n");
	}

	image_i16_t image = { .w = 0, .h = 0, .pixels = image_buffer, };
	//read_image("dump_processed_palm_2.csv", &image);
	//read_image("dump_processed_palm_2_ov.csv", &image);
	//read_image("WT0031_ImageDump_2018_1120_000.csv", &image);
	read_image(path, &image);
	//printf("#row = %d, #col = %d\n", image.h, image.w);
	//dump_i16_image("source", "%3d ", image.pixels, image.w, image.h);
	//dump_i16_image("source", "%2d,", image.pixels, image.w, image.h);

	image_i16_t frame_image = { .w = FRMWIDTH, .h = FRMHEIGHT, .pixels = frame_buffer, };

	unsigned offset_x = 0;// 84 - 32;
	unsigned offset_y = 0;// 48 - 40;
	paste_image(&frame_image, &image, offset_x, offset_y);
	//dump_i16_image("frame image", "%3d ", frame_image.pixels, frame_image.w, frame_image.h);
	dump_i16_image("frame image", "%2d,", frame_image.pixels, frame_image.w, frame_image.h);

	rect_t roi = 
	{
		//.top_left = { .x = offset_x, .y = offset_y, },
		//.bottom_right = { .x = offset_x + image.w, .y = offset_y + image.h, },
		.top_left = { .x = 0,.y = 0, },
		.bottom_right = {.x = FRMWIDTH, .y = FRMHEIGHT, },
	}; 

	
	//printf("ROI = (%d,%d)-(%d,%d)\n", roi.left, roi.top, roi.right, roi.bottom);

	if (fp_log != NULL)
		fprintf(fp_log, "ROI = (%d,%d)-(%d,%d)\n", roi.left, roi.top, roi.right, roi.bottom);

	//rect_t regions[N_REGIONS_MAX];
	int n = algo_label8cc_and_extract_boxes(regions, frame_image.pixels, labeled, rg_threshold, &roi);
	int i;

	dump_i16_image("labeled and flattened", "%2d ", labeled, FRMWIDTH, FRMHEIGHT);

	if (n > 0)
	{
		if (fp_log != NULL)
			fprintf(fp_log, "Bounding boxes:\n");
		for (i = 0; i < n; i++)
		{
			//printf("%2d: (%d,%d)-(%d,%d)\n", i, regions[i].top_left.x, regions[i].top_left.y, regions[i].bottom_right.x, regions[i].bottom_right.y);
			if (fp_log != NULL)
				fprintf(fp_log, "%2d: (%d,%d)-(%d,%d)\n", i, regions[i].top_left.x, regions[i].top_left.y, regions[i].bottom_right.x, regions[i].bottom_right.y);
		}
	}
	else if (n == 0)
	{
		if (fp_log != NULL)
			fprintf(fp_log, "No region.\n");
		//printf("No region.\n");
	}
	else
	{
		printf("Error : algo_label8cc_and_extract_boxes() returns %d\n", n);
	}

	if (fp_log != NULL)
		fclose(fp_log);

	return n;
}
