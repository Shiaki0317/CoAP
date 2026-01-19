CC=gcc
CFLAGS=-Wall -Wextra -std=c11 -g
INCLUDES=-I/usr/local/include

sample_separate_server: sample_separate_server.c
	$(CC) $(CFLAGS) $(INCLUDES) sample_separate_server.c -o sample_separate_server -lcoap-3

clean :
	rm -f common.o