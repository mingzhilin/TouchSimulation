#include <string.h>
#include <stdio.h>
#include "region.h"

extern FILE * fp_log;
extern int FRMWIDTH;
extern int FRMHEIGHT;
extern int FRMSIZE;

static inline int max(int a, int b) { return a > b ? a : b; }
static inline int min(int a, int b) { return a < b ? a : b; }

#define INVALID_XY	0xFF

#define LABEL_GENERATOR_MAX_SIZE	(64)
#define WEST(x) (*((x) - 1))
#define EAST(x) (*((x) + 1))

//---------------------------------------- GLOBALS ---------------------------------------------------------
typedef struct {
	uint8_t E[LABEL_GENERATOR_MAX_SIZE+1];
	uint8_t nextLabel;
	uint8_t max_size;
} RegionLabeler;

RegionLabeler TheLabeler;

//------------------------------------ LOCAL FUNCTIONS -----------------------------------------------------
uint8_t algo_processing_cc8l_union(uint8_t *E, uint8_t i, uint8_t j);
uint8_t algo_processing_cc8l_find(const uint8_t *E, uint8_t i);
void algo_processing_cc8l_set(uint8_t *E, uint8_t i, uint8_t root);
void algo_processing_cc8l_flattenLabel(uint8_t *E, unsigned sizeE);
int algo_processing_extract_connected_regions_bounding_box(const uint16_t *labelMap, rect_t *boxes, const rect_t *roi);

//------------------------------------- IMPLEMENTATION ------------------------------------------------------
/**
 * @brief Combine the two trees containing i and j
 * @param E Label equivalence array
 * @returns Index of the common root in the array E
 */
uint8_t algo_processing_cc8l_union(uint8_t *E, uint8_t i, uint8_t j)
{
	uint8_t root, rootj;

	root = algo_processing_cc8l_find(E, i);

	if (i != j)
	{
		rootj = algo_processing_cc8l_find(E, j);
		if (root > rootj) root = rootj;

		algo_processing_cc8l_set(E, i, root);
		algo_processing_cc8l_set(E, j, root);
	}

	return root;
}

/**
 * @brief Find the root of the tree containing node @c i.
 * @returns The root of the tree
 */
uint8_t algo_processing_cc8l_find(const uint8_t *E, uint8_t i)
{
	uint8_t root = i;
	while (E[root] < root) root = E[root];
	return root;
}

/**
 * @brief Compresses a tree by setting all nodes to point to a new root
 */
void algo_processing_cc8l_set(uint8_t *E, uint8_t i, uint8_t root)
{
	uint8_t current = i;
	while (E[current] > root)
	{
		uint8_t j = E[current];
		E[current] = root;
		current = j;
	}
}

/**
 * Flatten the union-find tree and relabel the components to have consecutive labels
 */
void algo_processing_cc8l_flattenLabel(uint8_t *E, unsigned sizeE)
{
	unsigned i, label;

	for (i = 0, label = 0; i < sizeE; ++i)
		if (E[i] < i) E[i] = E[E[i]];
		else E[i] = label++;
}

/**
 * Get a new label from a labeler
 */
uint8_t ccl_newLabel(RegionLabeler *labels)
{
	unsigned newLabel = labels->nextLabel++;

	// Avoid overflows in the number of labels
	if (newLabel > labels->max_size) newLabel = labels->max_size;

	labels->E[newLabel] = newLabel;
	return newLabel;
}

/**
 * Set the correct initial set for a RegionLabeler
 */
void ccl_init_region_labeler(RegionLabeler *labeler)
{
	labeler->nextLabel = 1;
	labeler->max_size = LABEL_GENERATOR_MAX_SIZE;
	memset(labeler->E, 0, sizeof(labeler->E));
}

/**
 * Connected component labeling algorithm using an array-based union-find structure.
 * src and dest are assumed to have the same size given by FRMWIDTH x FRMHEIGHT.
 *
 * Note that the algorithm does use only 1 pass and that it is <strong>unsafe</strong> to call it in place (i.e., @c{src == dest}).
 *
 * @param src Input image to analyze
 * @param dest Image of connected labels
 * @param fgThreshold All pixels strictly below this threshold are considered as background pixels
 * @param roi Region of interest inside the input image. Both input and output images are assumed to have the same size.
 */
int algo_processing_label8cc(const int16_t *src, uint16_t *dest, int16_t fgThreshold, rect_t const *roi)
{
	size_t x, y, x_min, x_max, y_min, y_max, region_count;
	uint16_t *p_dest, *p_north;
	int16_t const *p_src;

	// Defensive programming
	if ((uint16_t *)src == dest) return 0;

	// Init the required structures
	ccl_init_region_labeler(&TheLabeler);

	x_min = roi->left;
	y_min = roi->top;
	x_max = roi->right;
	y_max = roi->bottom;
	memset(dest, 0, FRMSIZE*sizeof(int16_t));

	// First pass
	p_dest = dest + y_min*FRMWIDTH + x_min;
	p_src  = src  + y_min*FRMWIDTH + x_min;
	
	if (*p_src++ >= fgThreshold) 
		*p_dest++ = ccl_newLabel(&TheLabeler);
	else 
		*p_dest++ = 0;

	for (x = x_min+1; x < x_max; ++x, ++p_dest)
	{
		if (*p_src++ < fgThreshold) *p_dest = 0;
		else {
			// Since we are in the first line, there is no N-xx neighbours, only W
			*p_dest = (WEST(p_dest) == 0 ? ccl_newLabel(&TheLabeler) : WEST(p_dest) );
		}
	}

	// Now, inside the image
	for (y = y_min+1; y < y_max; ++y)
	{
		p_north = dest + (y-1)*FRMWIDTH + x_min;
		p_dest  = dest +     y*FRMWIDTH + x_min;
		p_src   = src  +     y*FRMWIDTH + x_min;

		// First column
		if (*p_src++ < fgThreshold) *p_dest++ = 0;
		else {
			if (*p_north > 0) *p_dest++ = *p_north;
			else {
				if ( EAST(p_north) > 0 ) *p_dest++ = EAST(p_north);
				else *p_dest++ = ccl_newLabel(&TheLabeler);
			}
		}
		++p_north;

		// Inside		

		for (x = x_min+1; x < x_max-1; ++x, ++p_north)
		{						
			if (*p_src < fgThreshold) *p_dest++ = 0;
			else {
				// Explore the NE-N-NW-W neighbors, in the order of the decision tree given in reference
				if (*p_north > 0) *p_dest++ = *p_north;
				else {
					int a, c, d;
					c = EAST(p_north);
					a = WEST(p_north);

					if (c == 0) {
						if (a > 0) *p_dest++ = a;
						else
						{
						    *p_dest = (WEST(p_dest) == 0 ? ccl_newLabel(&TheLabeler) : WEST(p_dest) );
						    p_dest ++;
						}
					} else {
						if (a > 0) *p_dest++ = algo_processing_cc8l_union(TheLabeler.E, a, c);
						else
						{
							d = WEST(p_dest);
							*p_dest = (d == 0 ? c : algo_processing_cc8l_union(TheLabeler.E, c, d) );
							p_dest ++;
						}
					}
				}
			}						
			p_src++;
		}
		// Last column: no NE neighbour
		if (*p_src++ < fgThreshold) *p_dest++ = 0;
		else {
			if (*p_north > 0) *p_dest++ = *p_north;
			else {
				if ( WEST(p_north ) > 0 ) *p_dest++ = WEST(p_north);
				else
				{
				    *p_dest = ( WEST(p_dest) == 0 ? ccl_newLabel(&TheLabeler) : WEST(p_dest) );
				    p_dest ++;
				}
			}
		}
	}

	//printf("# labeled = %d\n", TheLabeler.nextLabel - 1);
	if (fp_log)
		fprintf(fp_log, "# labeled = %d\n", TheLabeler.nextLabel - 1);
	dump_i16_image("labeled", "%2d ", (int16_t *)dest, FRMWIDTH, y_max);

	// Flatten the label array
	algo_processing_cc8l_flattenLabel(TheLabeler.E, TheLabeler.max_size+1);

	// Also flatten the label image
	region_count = 0;
	for (y = y_min; y < y_max; ++y)
	{
		uint16_t *p_labels = dest + y*FRMWIDTH + x_min;

		for (x = x_min; x < x_max; ++x, ++p_labels)
		{
			if (*p_labels > 0)
			{
				*p_labels = TheLabeler.E[*p_labels];
				region_count = max(region_count, *p_labels);
			}
		}
	}
	return region_count;
}

/**
 * Init the region statistics to values that are directly usable in the labeling routine
 */
void init_regions_boxes(rect_t* const destBoxes, uint16_t maxRegionCount)
{
	size_t i;

	for (i = 0; i < maxRegionCount; ++i)
	{
		rect_t *theRect = destBoxes + i;

		theRect->left = INVALID_XY;
		theRect->top = INVALID_XY;
		theRect->right = 0;
		theRect->bottom = 0;
	}
}

/**
 * Convenience function to label an image and extract the boxes of the detected connected regions
 * @see algo_processing_label8cc algo_processing_extract_connected_regions_bounding_box
 */
int algo_label8cc_and_extract_boxes(rect_t *boundingBoxes, const int16_t *src, uint16_t *dest, int fgThreshold, const rect_t *roi)
{
	if (roi->top < 0 || roi->left < 0 || roi->right > FRMWIDTH || roi->bottom > FRMHEIGHT)
		return -1;

	// Apply CC labeling
	int numberOfRegions = algo_processing_label8cc(src, dest, fgThreshold, roi);

	// Invalidate output
	init_regions_boxes(boundingBoxes, N_REGIONS_MAX);

	// Extract properties
	if (numberOfRegions > 0 && numberOfRegions <= N_REGIONS_MAX)
	{
		numberOfRegions = algo_processing_extract_connected_regions_bounding_box(dest, boundingBoxes, roi);
	}

	return min(numberOfRegions, N_REGIONS_MAX);
}

/**
 * Computes the bounding box of the connected regions detected by the labeling procedure
 */
int algo_processing_extract_connected_regions_bounding_box(const uint16_t *labelMap, rect_t *boxes, const rect_t *roi)
{
	int x, y, x_min, y_min, x_max, y_max, numberOfRegions;

	// Init everything
	x_min = roi->left;
	y_min = roi->top;
	x_max = roi->right;
	y_max = roi->bottom;

	numberOfRegions = 0;

	// Loop over the label image	
	for (y = y_min; y < y_max; ++y)
	{
		const uint16_t *p_labels = labelMap + y*FRMWIDTH + x_min;

		for (x = x_min; x < x_max; ++x, ++p_labels)
		{
			if (*p_labels > 0 && *p_labels <= N_REGIONS_MAX)
			{
				rect_t * box = (boxes + *p_labels - 1);

				if (box->left == INVALID_XY) ++numberOfRegions;

				if (box->left > x) 	
					box->left = x;

				if (box->top > y)
					box->top = y;

#if 0
				if (box->right < x+1)
					box->right = (x + 1);

				if (box->bottom < y+1)
					box->bottom = (y + 1);
#else
				if (box->right < x)
					box->right = x;

				if (box->bottom < y)
					box->bottom = y;
#endif
			}
		}
	}

	return numberOfRegions;
}

