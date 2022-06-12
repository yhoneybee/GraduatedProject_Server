using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using MyPacket;

namespace GraduatedProject_Server
{
    public class BufferManager : Singleton<BufferManager>
    {
        int m_num_bytes;
        byte[]? m_buffer;
        Stack<int>? m_free_index_pool;
        int m_current_index;
        int m_buffer_size;

        public BufferManager()
        {

        }

        protected override void Init()
        {
            int maxConnectCount = 100;

            m_num_bytes = maxConnectCount * Defines.SOCKET_BUFFER_SIZE * 2;
            m_current_index = 0;
            m_buffer_size = Defines.SOCKET_BUFFER_SIZE;
            m_free_index_pool = new Stack<int>();
            m_buffer = new byte[m_num_bytes];
        }

        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (m_free_index_pool!.Count > 0)
            {
                args.SetBuffer(m_buffer, m_free_index_pool.Pop(), m_buffer_size);
            }
            else
            {
                if (m_num_bytes < (m_current_index + m_buffer_size))
                {
                    return false;
                }
                args.SetBuffer(m_buffer, m_current_index, m_buffer_size);
                m_current_index += m_buffer_size;
            }
            return true;
        }

        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            if (args == null)
                return;
            m_free_index_pool!.Push(args.Offset);
        }
    }
}
