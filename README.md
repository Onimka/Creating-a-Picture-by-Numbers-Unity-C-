![image](https://github.com/user-attachments/assets/c6347490-39be-429e-ad6e-c7d9c18e3a5e)

▎Numbered Coloring Application

This application allows users to create numbered coloring pages from photographs and images.

▎Creating a Coloring Page

The process consists of the following steps:

1. Image Upload: Users can upload their desired image.

2. Color Palette Setup: The user specifies the desired number of colors in the palette. The algorithm then calculates an optimal palette for the uploaded image based on the specified number of colors.

3. Blurring Level Adjustment: Users set the level of blurring needed to reduce the visibility of small details in the image.

4. Image Posterization: The image is posterized, meaning that it is transformed into an image with a limited color palette based on the specified colors.

5. Final Cleanup: The image is cleaned up to remove any remaining small details.

6. Results Generation:

   • Numbered Coloring Page: The main image is a blank canvas with outlines drawn on it. Numerical color labels are placed within all outlines, except for those that are too small to accommodate them.

   • Completed Coloring Reference: This auxiliary image represents the finished numbered coloring page after coloring. It is particularly useful for small outlines that could not have numerical color labels due to their size. The colors in this reference can help users identify which color to use. For better readability, numerical labels are not applied to this image.

   • Coloring Table: A set of colors required for coloring, with each color assigned a corresponding numerical label.
