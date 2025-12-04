import React, { useEffect, useState, useRef } from 'react';
import { recipientService } from '../services/api';
import { Recipient, RecipientUploadResult } from '../types';
import { Upload, Users, FileSpreadsheet, Check } from 'lucide-react';

const RecipientsPage: React.FC = () => {
  const [recipients, setRecipients] = useState<Recipient[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [uploadResult, setUploadResult] = useState<RecipientUploadResult | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(50);
  const [totalPages, setTotalPages] = useState(1);
  const [totalRecipients, setTotalRecipients] = useState(0);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    loadRecipients(1);
  }, []);

  const loadRecipients = async (pageToLoad: number) => {
    try {
      setLoading(true);
      const data = await recipientService.getAll(pageToLoad, pageSize);
      setRecipients(data.items);
      setPage(data.page);
      setTotalPages(data.totalPages);
      setTotalRecipients(data.totalItems);
    } catch (error) {
      console.error('Failed to load recipients', error);
      alert('Unable to load recipients. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      const file = e.target.files[0];
      setUploading(true);
      setUploadResult(null);
      try {
        const result = await recipientService.uploadCsv(file);
        setUploadResult(result);
        await loadRecipients(1); // Refresh list
      } catch (error) {
        console.error('Upload failed', error);
        alert('Upload failed. Please check the file format.');
      } finally {
        setUploading(false);
        if (fileInputRef.current) fileInputRef.current.value = '';
      }
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Recipients</h1>
          <p className="text-gray-500">Manage your email list</p>
        </div>
        <div className="relative">
          <input
            type="file"
            accept=".csv"
            ref={fileInputRef}
            onChange={handleFileChange}
            className="hidden"
            id="csv-upload"
          />
          <label
            htmlFor="csv-upload"
            className={`flex items-center gap-2 bg-white border border-gray-300 text-gray-700 px-4 py-2 rounded-lg hover:bg-gray-50 cursor-pointer shadow-sm transition-all ${uploading ? 'opacity-50 pointer-events-none' : ''}`}
          >
            {uploading ? (
               <span className="w-5 h-5 border-2 border-gray-300 border-t-blue-600 rounded-full animate-spin" />
            ) : (
               <Upload size={18} />
            )}
            <span>Upload CSV</span>
          </label>
        </div>
      </div>

      <div className="flex justify-end items-center gap-3 text-sm text-gray-500">
        <span>
          Page {page} of {totalPages}
        </span>
        <div className="flex gap-2">
          <button
            onClick={() => loadRecipients(Math.max(1, page - 1))}
            disabled={page === 1 || loading}
            className="px-3 py-1 rounded-lg border border-gray-300 bg-white disabled:opacity-50"
          >
            Previous
          </button>
          <button
            onClick={() => loadRecipients(Math.min(totalPages, page + 1))}
            disabled={page === totalPages || loading}
            className="px-3 py-1 rounded-lg border border-gray-300 bg-white disabled:opacity-50"
          >
            Next
          </button>
        </div>
      </div>

      {/* Upload Result Notification */}
      {uploadResult && (
        <div className="bg-green-50 border border-green-200 rounded-xl p-4 flex items-start gap-3 animate-in fade-in slide-in-from-top-2">
          <div className="p-1 bg-green-100 text-green-600 rounded-full">
            <Check size={16} />
          </div>
          <div>
            <h3 className="font-semibold text-green-900">Upload Completed</h3>
            <p className="text-sm text-green-800 mt-1">
              Processed {uploadResult.totalRows} rows.
              <span className="font-bold ml-1">{uploadResult.inserted} inserted</span>,
              <span className="text-green-700 ml-1">{uploadResult.skippedInvalid} skipped/invalid</span>.
            </p>
          </div>
        </div>
      )}

      {/* Recipient List */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-100 flex justify-between items-center">
          <div className="font-semibold text-gray-800 flex items-center gap-2">
            <Users size={18} className="text-gray-400" />
            All Recipients
          </div>
          <span className="text-xs bg-gray-100 text-gray-600 px-2 py-1 rounded-md">
            Total: {totalRecipients}
          </span>
        </div>
        
        {loading ? (
          <div className="p-10 text-center text-gray-500">Loading recipients...</div>
        ) : recipients.length === 0 ? (
          <div className="p-12 flex flex-col items-center justify-center text-gray-400">
            <FileSpreadsheet size={48} className="mb-4 text-gray-200" />
            <p>No recipients found.</p>
            <p className="text-sm">Upload a CSV file to get started.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
             <table className="w-full text-left text-sm text-gray-600">
              <thead className="bg-gray-50 text-gray-500 uppercase font-medium text-xs">
                <tr>
                  <th className="px-6 py-3">Email</th>
                  <th className="px-6 py-3">First Name</th>
                  <th className="px-6 py-3">Last Name</th>
                  <th className="px-6 py-3">Added</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {recipients.map((r) => (
                  <tr key={r.id} className="hover:bg-gray-50">
                    <td className="px-6 py-3 font-medium text-gray-900">{r.email}</td>
                    <td className="px-6 py-3">{r.firstName || '-'}</td>
                    <td className="px-6 py-3">{r.lastName || '-'}</td>
                    <td className="px-6 py-3 text-gray-400">
                        {r.createdAt ? new Date(r.createdAt).toLocaleDateString() : '-'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};

export default RecipientsPage;
