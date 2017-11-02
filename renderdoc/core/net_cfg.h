
#pragma once

/* Abstract socket config */
// Enable defines to allow abstract socket servers on the given platform
#if ENABLED(RDOC_LINUX)
//#define LINUX_ABSTRACT_SOCKET
#endif

#if ENABLED(RDOC_ANDROID)
# define ANDROID_ABSTRACT_SOCKET
#endif

// Enable defines to allow abstract socket client connection on the given platform
#if ENABLED(RDOC_LINUX)
//#define LINUX_ABSTRACT_SOCKET_CLIENT
#endif

#if ENABLED(RDOC_ANDROID)
//#define ANDROID_ABSTRACT_SOCKET_CLIENT
#endif


