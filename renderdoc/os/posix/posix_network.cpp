/******************************************************************************
 * The MIT License (MIT)
 *
 * Copyright (c) 2016-2017 Baldur Karlsson
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 ******************************************************************************/

#include <errno.h>
#include <fcntl.h>
#include <netdb.h>
#include <netinet/in.h>
#include <netinet/tcp.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/socket.h>
#include <sys/types.h>
#include <sys/un.h>
#include <unistd.h>
#include <string>
#include "os/os_specific.h"
#include "serialise/string_utils.h"
#include "core/net_cfg.h"

using std::string;

namespace Network
{
void Init()
{
}

void Shutdown()
{
}

Socket::~Socket()
{
  Shutdown();
}

void Socket::Shutdown()
{
  if(Connected())
  {
    shutdown((int)socket, SHUT_RDWR);
    fprintf(stderr, "CLOSING\n");
    close((int)socket);
    socket = -1;
  }
}

bool Socket::Connected() const
{
  return (int)socket != -1;
}

uint32_t Socket::GetRemoteIP() const
{
  sockaddr_in addr = {};
  socklen_t len = sizeof(addr);

  getpeername((int)socket, (sockaddr *)&addr, &len);

  return ntohl(addr.sin_addr.s_addr);
}

Socket *Socket::AcceptClient(bool wait)
{
  do
  {
    int s = accept(socket, NULL, NULL);

    if(s != -1)
    {
      int flags = fcntl(s, F_GETFL, 0);
      fcntl(s, F_SETFL, flags | O_NONBLOCK);

      int nodelay = 1;
      setsockopt(s, IPPROTO_TCP, TCP_NODELAY, (char *)&nodelay, sizeof(nodelay));

      return new Socket((ptrdiff_t)s);
    }

    int err = errno;

    if(err != EWOULDBLOCK && err != EAGAIN)
    {
      RDCWARN("accept: %d", err);
      Shutdown();
    }

    Threading::Sleep(4);
  } while(wait);

  return NULL;
}

bool Socket::SendDataBlocking(const void *buf, uint32_t length)
{
  if(length == 0)
    return true;

  uint32_t sent = 0;

  char *src = (char *)buf;

  int flags = fcntl(socket, F_GETFL, 0);
  fcntl(socket, F_SETFL, flags & ~O_NONBLOCK);

  while(sent < length)
  {
    int ret = send(socket, src, length - sent, 0);

    if(ret <= 0)
    {
      int err = errno;

      if(err == EWOULDBLOCK || err == EAGAIN)
      {
        ret = 0;
      }
      else
      {
        RDCWARN("send: %d", err);
        Shutdown();
        return false;
      }
    }

    sent += ret;
    src += ret;
  }

  flags = fcntl(socket, F_GETFL, 0);
  fcntl(socket, F_SETFL, flags | O_NONBLOCK);

  RDCASSERT(sent == length);

  return true;
}

bool Socket::IsRecvDataWaiting()
{
  char dummy;
  int ret = recv(socket, &dummy, 1, MSG_PEEK);

  if(ret == 0)
  {
    Shutdown();
    return false;
  }
  else if(ret <= 0)
  {
    int err = errno;

    if(err == EWOULDBLOCK || err == EAGAIN)
    {
      ret = 0;
    }
    else
    {
      RDCWARN("recv: %d", err);
      Shutdown();
      return false;
    }
  }

  return ret > 0;
}

bool Socket::RecvDataBlocking(void *buf, uint32_t length)
{
  if(length == 0)
    return true;

  uint32_t received = 0;

  char *dst = (char *)buf;

  int flags = fcntl(socket, F_GETFL, 0);
  fcntl(socket, F_SETFL, flags & ~O_NONBLOCK);

  while(received < length)
  {
    int ret = recv(socket, dst, length - received, 0);

    if(ret == 0)
    {
      Shutdown();
      return false;
    }
    else if(ret <= 0)
    {
      int err = errno;

      if(err == EWOULDBLOCK || err == EAGAIN)
      {
        ret = 0;
      }
      else
      {
        RDCWARN("recv: %d", err);
        Shutdown();
        return false;
      }
    }

    received += ret;
    dst += ret;
  }

  flags = fcntl(socket, F_GETFL, 0);
  fcntl(socket, F_SETFL, flags | O_NONBLOCK);

  RDCASSERT(received == length);

  return true;
}

Socket *CreateServerSocket(const char *bindaddr, uint16_t port, int queuesize)
{
#if defined(LINUX_ABSTRACT_SOCKET) || defined(ANDORID_ABSTRACT_SOCKET)
  char socketName[256] = {0};
  StringFormat::snprintf(socketName, 256, "/renderdoc/%d", port);
  int s = socket(AF_UNIX, SOCK_STREAM, 0);

  if (s == -1) {
    return NULL;
  }

  sockaddr_un addr;
  RDCEraseEl(addr);

  addr.sun_family = AF_UNIX;
  // first char is '\0'
  addr.sun_path[0] = '\0';
  strncpy(addr.sun_path + 1, socketName, strlen(socketName));

  int result = bind(s, (sockaddr *)&addr,  offsetof(sockaddr_un, sun_path) + 1 + strlen(socketName));
  if (result == -1)
  {
    RDCWARN("Failed to create abstract socket: %s", socketName);
    close(s);
    return NULL;
  }
  RDCWARN("Created and bind socket: %d", s);

  result = listen(s, queuesize);
  if(result == -1)
  {
    RDCWARN("Failed to listen on %s", addr.sun_path + 1);
    close(s);
    return NULL;
  }
#else
  int s = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);

  int yes = 1;
  setsockopt(s, SOL_SOCKET, SO_REUSEADDR, &yes, sizeof(yes));

  if(s == -1)
    return NULL;

  sockaddr_in addr;
  RDCEraseEl(addr);

  hostent *hp = gethostbyname(bindaddr);

  addr.sin_family = AF_INET;
  memcpy(&addr.sin_addr, hp->h_addr, hp->h_length);
  addr.sin_port = htons(port);

  int result = bind(s, (sockaddr *)&addr, sizeof(addr));
  if(result == -1)
  {
    RDCWARN("Failed to bind to %s:%d - %d", bindaddr, port, errno);
    close(s);
    return NULL;
  }

  result = listen(s, queuesize);
  if(result == -1)
  {
    RDCWARN("Failed to listen on %s:%d - %d", bindaddr, port, errno);
    close(s);
    return NULL;
  }
#endif

  int flags = fcntl(s, F_GETFL, 0);
  fcntl(s, F_SETFL, flags | O_NONBLOCK);

  return new Socket((ptrdiff_t)s);
}

Socket *CreateClientSocket(const char *host, uint16_t port, int timeoutMS)
{
#if defined(LINUX_ABSTRACT_SOCKET_CLIENT) || defined(ANDROID_ABSTRACT_SOCKET_CLIENT)
  char socketName[256] = {0};
  StringFormat::snprintf(socketName, 256, "/renderdoc/%d", port);

  sockaddr_un addr;
  RDCEraseEl(addr);

  addr.sun_family = AF_UNIX;
  // first char is '\0'
  strcpy(addr.sun_path + 1, socketName);

  int s = socket(AF_UNIX, SOCK_STREAM, 0);

  if (s == -1)
    return NULL;

  int flags = fcntl(s, F_GETFL, 0);
  fcntl(s, F_SETFL, flags | O_NONBLOCK);

  int result = connect(s, (sockaddr *)&addr, sizeof(addr));
  if(result == -1)
  {
    fd_set set;
    FD_ZERO(&set);
    FD_SET(s, &set);

    int err = errno;

    if(err == EWOULDBLOCK || err == EINPROGRESS)
    {
      timeval timeout;
      timeout.tv_sec = (timeoutMS / 1000);
      timeout.tv_usec = (timeoutMS % 1000) * 1000;
      result = select(s + 1, NULL, &set, NULL, &timeout);

      if(result <= 0)
      {
        RDCDEBUG("connect timed out");
        close(s);
        return NULL;
      }
    }
    else
    {
      RDCWARN("Error connecting to %s:%d - %d", host, port, err);
      close(s);
      return NULL;
    }
  }

  int nodelay = 1;
  setsockopt(s, IPPROTO_TCP, TCP_NODELAY, (char *)&nodelay, sizeof(nodelay));

  return new Socket((ptrdiff_t)s);
#else
  char portstr[7] = {0};
  StringFormat::snprintf(portstr, 6, "%d", port);

  addrinfo hints;
  RDCEraseEl(hints);
  hints.ai_family = AF_INET;
  hints.ai_socktype = SOCK_STREAM;
  hints.ai_protocol = IPPROTO_TCP;

  addrinfo *result = NULL;
  getaddrinfo(host, portstr, &hints, &result);

  for(addrinfo *ptr = result; ptr != NULL; ptr = ptr->ai_next)
  {
    int s = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);

    if(s == -1)
      return NULL;

    int flags = fcntl(s, F_GETFL, 0);
    fcntl(s, F_SETFL, flags | O_NONBLOCK);

    int result = connect(s, ptr->ai_addr, (int)ptr->ai_addrlen);
    if(result == -1)
    {
      fd_set set;
      FD_ZERO(&set);
      FD_SET(s, &set);

      int err = errno;

      if(err == EWOULDBLOCK || err == EINPROGRESS)
      {
        timeval timeout;
        timeout.tv_sec = (timeoutMS / 1000);
        timeout.tv_usec = (timeoutMS % 1000) * 1000;
        result = select(s + 1, NULL, &set, NULL, &timeout);

        if(result <= 0)
        {
          RDCDEBUG("connect timed out");
          close(s);
          continue;
        }
      }
      else
      {
        RDCWARN("Error connecting to %s:%d - %d", host, port, err);
        close(s);
        continue;
      }
    }

    int nodelay = 1;
    setsockopt(s, IPPROTO_TCP, TCP_NODELAY, (char *)&nodelay, sizeof(nodelay));

    return new Socket((ptrdiff_t)s);
  }

  RDCWARN("Failed to connect to %s:%d", host, port);
  return NULL;
#endif
}

bool ParseIPRangeCIDR(const char *str, uint32_t &ip, uint32_t &mask)
{
  uint32_t a = 0, b = 0, c = 0, d = 0, num = 0;

  int ret = sscanf(str, "%u.%u.%u.%u/%u", &a, &b, &c, &d, &num);

  if(ret != 5 || a > 255 || b > 255 || c > 255 || d > 255 || num > 32)
  {
    ip = 0;
    mask = 0;
    return false;
  }

  ip = MakeIP(a, b, c, d);

  if(num == 0)
  {
    mask = 0;
  }
  else
  {
    num = 32 - num;
    mask = ((~0U) >> num) << num;
  }

  return true;
}
};
