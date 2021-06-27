#include <iostream>
#include <math.h>
#include <stdlib.h>
#include<string.h>
#include<msclr\marshal_cppstd.h>
#include<mpi.h>
#include<stdio.h>
#include <ctime>// include this header 
#pragma once

#using <mscorlib.dll>
#using <System.dll>
#using <System.Drawing.dll>
#using <System.Windows.Forms.dll>
using namespace std;
using namespace msclr::interop;

int* inputImage(int* w, int* h, System::String^ imagePath) //put the size of image in w & h
{
	int* input;


	int OriginalImageWidth, OriginalImageHeight;

	//*********************************************************Read Image and save it to local arrayss*************************	
	//Read Image and save it to local arrayss

	System::Drawing::Bitmap BM(imagePath);

	OriginalImageWidth = BM.Width;
	OriginalImageHeight = BM.Height;
	*w = BM.Width;
	*h = BM.Height;
	int *Red = new int[BM.Height * BM.Width];
	int *Green = new int[BM.Height * BM.Width];
	int *Blue = new int[BM.Height * BM.Width];
	input = new int[BM.Height*BM.Width];
	for (int i = 0; i < BM.Height; i++)
	{
		for (int j = 0; j < BM.Width; j++)
		{
			System::Drawing::Color c = BM.GetPixel(j, i);

			Red[i * BM.Width + j] = c.R;
			Blue[i * BM.Width + j] = c.B;
			Green[i * BM.Width + j] = c.G;

			input[i*BM.Width + j] = ((c.R + c.B + c.G) / 3); //gray scale value equals the average of RGB values

		}

	}
	return input;
}


void createImage(int* image, int width, int height, int index)
{
	System::Drawing::Bitmap MyNewImage(width, height);


	for (int i = 0; i < MyNewImage.Height; i++)
	{
		for (int j = 0; j < MyNewImage.Width; j++)
		{
			//i * OriginalImageWidth + j
			if (image[i*width + j] < 0)
			{
				image[i*width + j] = 0;
			}
			if (image[i*width + j] > 255)
			{
				image[i*width + j] = 255;
			}
			System::Drawing::Color c = System::Drawing::Color::FromArgb(image[i*MyNewImage.Width + j], image[i*MyNewImage.Width + j], image[i*MyNewImage.Width + j]);
			MyNewImage.SetPixel(j, i, c);
		}
	}
	MyNewImage.Save("..//Data//Output//outputRes" + index + ".png");
	cout << "result Image Saved " << index << endl;
}


int main()
{
	int ImageWidth = 4, ImageHeight = 4;

	int start_s, stop_s, TotalTime = 0;

	System::String^ imagePath;
	std::string img;
	img = "..//Data//Input//test.png";

	imagePath = marshal_as<System::String^>(img);
	int* imageData = inputImage(&ImageWidth, &ImageHeight, imagePath);
	

	start_s = clock();
	MPI_Init(NULL, NULL);
	int rank, size;
	MPI_Comm_rank(MPI_COMM_WORLD, &rank);
	MPI_Comm_size(MPI_COMM_WORLD, &size);
	int totalImgSize = ImageWidth * ImageHeight;
	int* PartialImage = new int[totalImgSize / size];
	int* final = new int[totalImgSize];
	int* Localimg = new int[totalImgSize / size];
	
	int kernal[9] = { 0,-1,0,-1,4,-1,0,-1,0 };

	int blocksOfPixels =(totalImgSize / size);
	MPI_Bcast(&kernal, 9, MPI_INT, 0, MPI_COMM_WORLD); // brodcasting kerenl
	MPI_Scatter(imageData, blocksOfPixels, MPI_INT, PartialImage, blocksOfPixels, MPI_INT, 0, MPI_COMM_WORLD);
	int index;
	int sum = 0;
	int MovedWidth;
	for (int i = 0; i < blocksOfPixels; i++) {
		sum = 0;
		index = i;
		MovedWidth = 1;
		for (int j = 0; j < 9; j++) {
			sum+= (PartialImage[index] * kernal[j]);
			if (MovedWidth % 3 == 0) {
				index += (ImageWidth - index);
			}
			else
			{
				index++;
			}
			MovedWidth++;
		}
		Localimg[i] = sum;
	}
	MPI_Gather(Localimg, blocksOfPixels, MPI_INT, final, blocksOfPixels, MPI_INT, 0, MPI_COMM_WORLD);

	if (rank == 0) {
		createImage(final, ImageWidth, ImageHeight, 3);
		stop_s = clock();
		TotalTime += (stop_s - start_s) / double(CLOCKS_PER_SEC) * 1000;
		cout << "time: " << TotalTime << endl;
	}
	MPI_Finalize();
	free(imageData);
	return 0;

}



