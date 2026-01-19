CC=gcc
CFLAGS=-Wall -Wextra -std=c11 -g
INCLUDES=-I/usr/local/include

sample_separate_client: sample_separate_client.c
	$(CC) $(CFLAGS) $(INCLUDES) sample_separate_client.c -o sample_separate_client -lcoap-3

clean :
	rm -f common.o